#include "sniffer.h"
#include "connection.h"
#include "utilities.h"
#include <inttypes.h>
#include "led.h"

extern struct status st;

wifi_promiscuous_filter_t filter={
        .filter_mask= WIFI_PROMIS_FILTER_MASK_MGMT
};

//Wipes old packets
static void clear_data()
{
    st.total_length = 0;
    st.count = 0;
}

//add elapsed time to timestamp received from server
static void calculate_timestamp(char *new_time)
{

    struct timeval elapsed = timeval_durationToNow(&st.client_time);
    struct timeval timestamp = timeval_add(&st.srv_time,&elapsed);
    struct tm *timestamp_str;
    timestamp_str = localtime(&timestamp.tv_sec);

    sprintf(new_time, "%d-%02d-%02dT%02d:%02d:%02d.%06ld+02:00",
        timestamp_str->tm_year + 1900,
        timestamp_str->tm_mon,
        timestamp_str->tm_mday,
        timestamp_str->tm_hour,
        timestamp_str->tm_min,
        timestamp_str->tm_sec,
        timestamp.tv_usec);
    new_time[TIME_LEN-7]='0';
    new_time[TIME_LEN-8]='0';
    new_time[TIME_LEN-9]='0';
    //printf("%s\n",new_time );
}

//promiscuous callback function, called each time a packet is sniffed
static void IRAM_ATTR promiscuous_rx_cb(void *buf, wifi_promiscuous_pkt_type_t type)
{
    char c;
    int i;
    char new_time[TIME_LEN + 1];
    char hash_str[HASH_LEN];
    char ssid[SSID_MAXLEN + 1];
    if (type != WIFI_PKT_MGMT)
        return;

    //to prevent creating nodes during sending process
    if (st.status_value != ST_SNIFFING)
        return;

    c = ((wifi_promiscuous_pkt_t *)buf)->payload[0] & 0xB0;
    if (c != 0)
        return;

    struct packet_info *new_node = &st.packet_list[st.count % MAX_QUEUE_LEN];
    if (new_node == NULL)
    {
        printf("ERROR pointing inside the packet list\n");
        esp_restart();
    }
    st.count += 1;

    //save mac address of sender device
    for (i = 0; i < 6; i++)
    {
        sprintf(new_node->mac + i * 3, "%02x", ((wifi_promiscuous_pkt_t *)buf)->payload[MAC_POS + i]);
        if (i != 5)
            sprintf(new_node->mac + 2 + i * 3, ":");
    }

    //save timestamp
    calculate_timestamp(new_time);
    strcpy(new_node->timestamp, new_time);
    
    //save rssi
    new_node->strength = (int)((wifi_promiscuous_pkt_t *)buf)->rx_ctrl.rssi;
	
	//setting mac string
    memset(&new_node->ssid, 0, SSID_MAXLEN);
    get_ssid((char *)((wifi_promiscuous_pkt_t *)buf)->payload, ((wifi_promiscuous_pkt_t *)buf)->rx_ctrl.sig_len, ssid);
    sprintf(new_node->ssid, ssid);
	
	//calculate and save hash
    char seq_num[SEQ_NUM_LEN + 1];
    get_seq_num((char *)((wifi_promiscuous_pkt_t *)buf)->payload, seq_num);
    //hash((const BYTE *)packet_info.payload, packet_info.len, (BYTE *)hash_str);
    hash(new_node->mac, new_node->ssid, seq_num, new_node->timestamp, (BYTE *)hash_str);
    for (i = 0; i < HASH_LEN; i++){
        sprintf((new_node->hash) + (i * 2), "%02x", hash_str[i]);
    }
	int val=MAC_LEN + strlen(new_node->ssid) + TIME_LEN + HASH_LEN*2 + JSON_FIELD_LEN;
    st.total_length += val;
}

void enable_promiscuous(){
    
    ESP_ERROR_CHECK(esp_wifi_set_promiscuous_filter(&filter));
    //Set callback function
    ESP_ERROR_CHECK(esp_wifi_set_promiscuous_rx_cb(promiscuous_rx_cb));
    bool mode=false;
    ESP_ERROR_CHECK(esp_wifi_set_promiscuous(true));
    while(mode!=true){
        ESP_ERROR_CHECK(esp_wifi_get_promiscuous(&mode));
        if(mode==true) break;
    }
}

void disable_promiscuous(){
    bool mode=true;
    ESP_ERROR_CHECK(esp_wifi_set_promiscuous(false));
    while(mode!=false){
        esp_wifi_get_promiscuous(&mode);
        if(mode==false) break;
    }  
}

static void start_collector_timer()
{
    ESP_ERROR_CHECK(esp_timer_start_once(st.timer, TIMER_USEC));
}

void start_sniffer()
{
    //Wiping old packets
    printf("\tWiping old packets\n");
    clear_data();

    //Starting collector timer
    printf("\tStarting collector timer: %ds\n",TIMER_USEC/1000);
    start_collector_timer();    
    
    //turning on promiscuos mode
    printf("\tTurning ON promiscuos mode\n");
    printf("Sniffing\n");
    enable_promiscuous();

    turn_led_on();
    st.status_value = ST_SNIFFING;
}

//handle the end of the timer: send data to server then reset timer and return sniffing
static void collector_timer_handle()
{
    //Turning off promiscuos mode
    printf("\tTurning OFF promiscuos mode\n");
    disable_promiscuous();

    turn_led_off();
    st.status_value = ST_SENDING_DATA;
    send_data();
    start_sniffer();
}

void initialize_sniffer()
{
    //Initialize collector timer
    esp_timer_create_args_t create_args;
    create_args.callback = collector_timer_handle;
    create_args.arg = NULL;
    create_args.dispatch_method = ESP_TIMER_TASK;
    create_args.name = "collector_timer\0";
    ESP_ERROR_CHECK(esp_timer_create(&create_args, &(st.timer)));
}



