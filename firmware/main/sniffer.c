#include "sniffer.h"
#include "connection.h"
#include "utilities.h"
#include <inttypes.h>

#define TASK_STACK_SIZE 10000

extern struct status st;

void event_handler_promiscuous(void *buf, wifi_promiscuous_pkt_type_t type)
{
 char c;
 int i;
 struct packet_node *new_node = NULL;
 char new_time[TIME_LEN+1];
 char hash_str[HASH_LEN];
 if(type != WIFI_PKT_MGMT)
  return;

 //to prevent creating nodes during sending process
 //if(st.status_value != ST_SNIFFING)
  //return;

 c = ((wifi_promiscuous_pkt_t *)buf)->payload[0] & 0xB0;
 if(c != 0)
  return;

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
  //printf("%02x", ((wifi_promiscuous_pkt_t *)buf)->payload[MAC_POS+i]);
  if(i != 5)
   sprintf(new_node->packet.mac+2+i*3, ":");
   //printf(":");
 }
 //printf("\n");

 //find_ssid(ssid);

 //save timestamp
 calculate_timestamp(new_time);
 strcpy(new_node->packet.timestamp, new_time);

 //save rssi
 new_node->packet.strength = (int)((wifi_promiscuous_pkt_t *)buf)->rx_ctrl.rssi;
 //printf("%s rssi:%d\n", new_time, new_node->packet.strength);

 //calculate and save hash
 hash((const BYTE *)((wifi_promiscuous_pkt_t *)buf)->payload, ((wifi_promiscuous_pkt_t *)buf)->rx_ctrl.sig_len, (BYTE *)hash_str);
 for(i = 0; i < HASH_LEN; i++)
  sprintf((new_node->packet.hash) + (i*2), "%02x", hash_str[i]);
 //printf("Hash: %s\n", new_node->packet.hash);

 sprintf(new_node->packet.ssid, "test_ssid");

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
 int n;

 st.total_length += JSON_HEAD_LEN+2;
 n = CODE_DATA;

 buf[0] = (char) (st.total_length >> 8);
 buf[1] = (char) (st.total_length & 0xff);

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
