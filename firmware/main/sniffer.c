#include "sniffer.h"
#include "connection.h"
#include "utilities.h"
#include <inttypes.h>

#define TASK_STACK_SIZE 10000

static void sniffer_task(void *parameters);
static void wifi_sniffer_cb(void *recv_buf, wifi_promiscuous_pkt_type_t type);

extern struct status st;
static QueueHandle_t sniffer_work_queue = NULL; //queue where the packets are put waiting to be managed by the task

//data struct that contains data passed by the callback function to the sniffer task
typedef struct {
 void *payload;
 uint32_t rssi;
 uint32_t len;
}sniffer_packet_into_t;

//function called by main program.
//Sets up queue filled by promiscuous event handler callback and used by sniffer task
esp_err_t sniffer_start()
{
 //setup queue
 sniffer_work_queue = xQueueCreate(50, sizeof(sniffer_packet_into_t));
 //setup sniffer task
 xTaskCreate(sniffer_task, "sniffer", 3000, NULL, 10, NULL);
 //set callback function
 esp_wifi_set_promiscuous_rx_cb(wifi_sniffer_cb);
 //set promiscuous mode
 ESP_ERROR_CHECK(esp_wifi_set_promiscuous(true));
 return ESP_OK;
}

//sniffer task that elaborates the packet coming from callback function
static void sniffer_task(void *parameters)
{
 int i;
 sniffer_packet_into_t packet_info;
 BaseType_t ret = 0;
 struct packet_node *new_node = NULL;
 char new_time[TIME_LEN+1];
 char hash_str[HASH_LEN];
 char ssid[SSID_MAXLEN+1];

 while(1)
 {
  ret = xQueueReceive(sniffer_work_queue, &packet_info, 100 / portTICK_PERIOD_MS);
  if (ret != pdTRUE) {
   continue;
  }

  //print the packet received
  print_raw_data((unsigned char *)packet_info.payload, packet_info.len);
  //save the packet in the list
  new_node = (struct packet_node *)malloc(sizeof(*new_node));
  if(new_node == NULL)
  {
   printf("ERROR IN MALLOC\n");
   esp_restart();
  }

  //save mac address of sender device
  for(i=0; i<6; i++)
  {
   sprintf(new_node->packet.mac+i*3, "%02x", ((char *)packet_info.payload)[MAC_POS+i]);
   if(i != 5)
    sprintf(new_node->packet.mac+2+i*3, ":");
  }

  //save timestamp
  calculate_timestamp(new_time);
  strcpy(new_node->packet.timestamp, new_time);

  //save rssi
  new_node->packet.strength = packet_info.rssi;

  //calculate and save hash
  hash((const BYTE *)packet_info.payload, packet_info.len, (BYTE *)hash_str);
  for(i = 0; i < HASH_LEN; i++)
   sprintf((new_node->packet.hash) + (i*2), "%02x", hash_str[i]);

  get_ssid((char *)packet_info.payload, packet_info.len, ssid);
  printf("SSID: %s\n", ssid);
  sprintf(new_node->packet.ssid, ssid);

  st.total_length += strlen(new_node->packet.ssid);
  st.total_length += MAC_LEN + TIME_LEN + (HASH_LEN*2) + JSON_FIELD_LEN;

  new_node->next = st.packet_list;
  st.packet_list = new_node;
 }
}

//promiscuous callback function, called each time a packet is sniffed
static void wifi_sniffer_cb(void *recv_buf, wifi_promiscuous_pkt_type_t type)
{
 char c;
 sniffer_packet_into_t packet_info;
 wifi_promiscuous_pkt_t *sniffer = (wifi_promiscuous_pkt_t *)recv_buf;

 if(type != WIFI_PKT_MGMT)
  return;

 //to prevent creating nodes during sending process
 if(st.status_value != ST_SNIFFING)
  return;

 c = sniffer->payload[0] & 0xB0;
 if(c != 0)
  return;

 //save signal strength and lenth of payload
 packet_info.rssi=sniffer->rx_ctrl.rssi;
 packet_info.len=sniffer->rx_ctrl.sig_len;
 //save payload
 wifi_promiscuous_pkt_t *backup = malloc(sniffer->rx_ctrl.sig_len);
 memcpy(backup, sniffer->payload, sniffer->rx_ctrl.sig_len);
 packet_info.payload = backup;
 //push the packet in the queue, if not full
 if (sniffer_work_queue) {
  if (xQueueSend(sniffer_work_queue, &packet_info, 100 / portTICK_PERIOD_MS) != pdTRUE) {
   printf("Sniffer work queue full!!\n");
  }
 }
 else
 {
  printf("Out of memory for promiscuous packet!!\n");
 }
}


void event_handler_promiscuous(void *buf, wifi_promiscuous_pkt_type_t type)
{
 char c;
 int i;
 struct packet_node *new_node = NULL;
 char new_time[TIME_LEN+1];
 char hash_str[HASH_LEN];
 char ssid[SSID_MAXLEN+1];
 if(type != WIFI_PKT_MGMT)
  return;

 //to prevent creating nodes during sending process
 if(st.status_value != ST_SNIFFING)
  return;

 c = ((wifi_promiscuous_pkt_t *)buf)->payload[0] & 0xB0;
 if(c != 0)
  return;

 //print raw packet received for debug purposes
 print_raw_data((unsigned char *)((wifi_promiscuous_pkt_t *)buf)->payload, ((wifi_promiscuous_pkt_t *)buf)->rx_ctrl.sig_len);

 new_node = (struct packet_node *)malloc(sizeof(*new_node));
 if(new_node == NULL)
 {
  printf("ERROR IN MALLOC\n");
  esp_restart();
 }

 //save mac address of sender device
 for(i=0; i<6; i++)
 {
  sprintf(new_node->packet.mac+i*3, "%02x", ((wifi_promiscuous_pkt_t *)buf)->payload[MAC_POS+i]);
  if(i != 5)
   sprintf(new_node->packet.mac+2+i*3, ":");
 }

 //save timestamp
 calculate_timestamp(new_time);
 strcpy(new_node->packet.timestamp, new_time);

 //save rssi
 new_node->packet.strength = (int)((wifi_promiscuous_pkt_t *)buf)->rx_ctrl.rssi;

 //calculate and save hash
 hash((const BYTE *)((wifi_promiscuous_pkt_t *)buf)->payload, ((wifi_promiscuous_pkt_t *)buf)->rx_ctrl.sig_len, (BYTE *)hash_str);
 for(i = 0; i < HASH_LEN; i++)
  sprintf((new_node->packet.hash) + (i*2), "%02x", hash_str[i]);

 get_ssid((char *)((wifi_promiscuous_pkt_t *)buf)->payload, ((wifi_promiscuous_pkt_t *)buf)->rx_ctrl.sig_len, ssid);
 printf("SSID: %s\n", ssid);
 sprintf(new_node->packet.ssid, ssid);

 st.total_length += strlen(new_node->packet.ssid);
 st.total_length += MAC_LEN + TIME_LEN + (HASH_LEN*2) + JSON_FIELD_LEN;

 new_node->next = st.packet_list;
 st.packet_list = new_node;
}

//sniffs packets then sends those to server in infinite loop
void sniffer()
{
 clear_data();
 start_timer();

 st.status_value = ST_SNIFFING;
 //ESP_ERROR_CHECK(esp_wifi_set_promiscuous(true));
}

void clear_data()
{
 struct packet_node *next;
 struct packet_node *p=st.packet_list;
 while(p!=NULL)
 {
  next=p->next;
  free(p);
  p=next;
 }
 st.total_length=0;
 st.packet_list = NULL;
}

void start_timer()
{
 esp_err_t ret;
 uint64_t usec=TIMER_USEC;
 ret = esp_timer_start_once(st.timer, usec);
 ESP_ERROR_CHECK( ret );
}

//handle the end of the timer: send data to server then reset timer and return sniffing
void timer_handle()
{
 st.status_value = ST_SENDING_DATA;
 //ESP_ERROR_CHECK(esp_wifi_set_promiscuous(false));
 //reconnect();
 print_data(); //SET ST_SENDING_DATA
 send_data();
 //disconnect();
 sniffer();
}

void print_data()
{
 char buf[BUFLEN];
 struct packet_node *p;
 int i = 0;
 int n, len;

 len = st.total_length;
 len += JSON_HEAD_LEN+2;
 n = CODE_DATA;

 buf[0] = (char) (len >> 8);
 buf[1] = (char) (len & 0xff);

 printf("%d ", n);
 printf("%02x%02x\n", buf[0], buf[1]);
 //printf("%lu", (long unsigned int)(st.total_length + JSON_LEN));
 printf("{\"Esp_Mac\":\"");
 get_device_mac(buf);
 printf("%s", buf);
 printf("\",\n\"Packets\":[\n");

 p = st.packet_list;
 while(p != NULL)
 {
  printf("{\"MAC\":\"%s\",\n\"SSID\":\"%s\",\n\"Timestamp\":\"%s\",\n\"Hash\":\"%s\",\n\"SignalStrength\":%04d,},\n", p->packet.mac, p->packet.ssid, p->packet.timestamp, p->packet.hash, p->packet.strength);
  p = p->next;
  if(i != 0 && (i++%50) == 0)
   usleep(1000000);
 }
 printf("]}\n");
}

//add elapsed time to timestamp received from server
void calculate_timestamp(char *new_time)
{
 time_t sec_elapsed = time(NULL)-st.client_time;
 time_t timestamp = st.srv_time + sec_elapsed;
 struct tm *timestamp_str;

 timestamp_str = localtime(&timestamp);
 sprintf(new_time, "%d-%02d-%02dT%02d:%02d:%02d.000000+02:00", timestamp_str->tm_year+1900, timestamp_str->tm_mon, timestamp_str->tm_mday, timestamp_str->tm_hour, timestamp_str->tm_min, timestamp_str->tm_sec);
}

void hash(const BYTE *v, int length, BYTE *hash_str)
{
 SHA256_CTX ctx;

 sha256_init(&ctx);
 sha256_update(&ctx, v, length);
 sha256_final(&ctx, hash_str);
}
