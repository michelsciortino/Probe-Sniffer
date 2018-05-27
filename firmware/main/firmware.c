//main source file for the MalnatiProject firmware of ESP32

#include "codes.h"
#include "connection.h"
#include "sniffer.h"

struct status st;

int test_global=0;

/* #HEX+ASCII PRINTER#
//print payload data in hex and ascii, for debug purposes
void print_data(unsigned char *buf, int len)
{
 int i, j, c, fill_spaces;
 for(i=0; i < len; i++)
 {
  printf("%02x ", buf[i] & 0xff);
  if(((i+1)%16)==0 && i!=0)
  {
   printf("  |  ");
   for(j=15; j>=0; j--)
   {
    c=buf[i-j];
    if(isprint(c))
     printf("%c", (char)c);
    else
     printf(".");
   }
   printf("\n");
  }
  else if(((i+1)%8)==0 && i!=0)
   printf("   ");
 }
 fill_spaces=16-(len%16);
 if(fill_spaces>=8)
  fill_spaces++;
 for(i=0; i<fill_spaces; i++)
  printf("   ");

 printf("  |  ");
 for(j=(len%16); j>=0; j--)
 {
  c=buf[len-j];
  if(isprint(c))
   printf("%c", (char)c);
  else
   printf(".");
 }
 printf("\n\n");
}

void print_packet_info_mgmt(wifi_promiscuous_pkt_t *pkt)
{
 printf("Strength: %d\nChannel: %d\nPayload:\n", pkt->rx_ctrl.rssi, pkt->rx_ctrl.channel);
 print_data(pkt->payload, pkt->rx_ctrl.sig_len);
}

//handle the packet sniffed in promiscuous mode (we are interested in management packets)
void event_handler_promiscuous(void *buf, wifi_promiscuous_pkt_type_t type)
{
 printf("Packet received\n"); //DEBUG
 switch(type)
 {
  case WIFI_PKT_MGMT:
   printf("MANAGEMENT\n"); //DEBUG
   //print_packet_info_mgmt((wifi_promiscuous_pkt_t *)buf);
   break;
  case WIFI_PKT_DATA:
   printf("DATA\n");
   //print_data(((wifi_promiscuous_pkt_t *)buf)->payload, ((wifi_promiscuous_pkt_t *)buf)->rx_ctrl.sig_len);
   //print_packet_info((wifi_promiscuous_pkt_t *)buf);
   break;
  default:
   printf("OTHER PACKET\n"); //DEBUG
   break;
 }
}

//setup promiscuous mode
int setup_promiscuous()
{
 wifi_init_config_t cfg = WIFI_INIT_CONFIG_DEFAULT();

 ESP_ERROR_CHECK(esp_wifi_init(&cfg));
 ESP_ERROR_CHECK(esp_wifi_set_promiscuous_rx_cb(event_handler_promiscuous));
 return 1;
}
*/

/*
int AP_protocol()
{
 if(detect_ESP_AP())
  return AP_PRESENT;

 TaskHandle_t tasks[2];

 //create task to broadcast my MAC (AP protocol)
 TaskHandle_t MAC_listen_handle = NULL;

 xTaskCreate( MAC_listen, "MAC_broadcast_listen", STACK_SIZE, NULL, tskIDLE_PRIORITY, &MAC_listen_handle );
 configASSERT( MAC_listem_handle );
 tasks[0] = MAC_listem_handle;

 //create task to listen to other MAC_broadcast
 TaskHandle_t MAC_broadcast_handle = NULL;

 xTaskCreate( MAC_send, "MAC_broadcast_send", STACK_SIZE, NULL, tskIDLE_PRIORITY, &MAC_send_handle );
 configASSERT( MAC_send_handle );
 tasks[1] = MAC_send_handle;

 //sleep(AP_TIMER);

 if(MAC_listen_handle != NULL)
  vTaskDelete(MAC_listen_handle);
 if(MAC_semd_handle != NULL)
  vTaskDelete(MAC_send_handle);
}
*/

//initialize the main structure
void initialize_st()
{
 st.status_value = ST_DISCONNECTED;
 strcpy(st.server_ip, "\0");
 st.port = -1;
 st.srv_time = 0;
 st.client_time = 0;
 st.packet_list = NULL;
 st.total_length = 0;
 st.timer = NULL;
 st.king = false;
}

//main function
void app_main()
{
 esp_err_t ret = nvs_flash_init();
 if (ret == ESP_ERR_NVS_NO_FREE_PAGES) {
  ESP_ERROR_CHECK(nvs_flash_erase());
  ret = nvs_flash_init();
 }
 ESP_ERROR_CHECK( ret );

 initialize_st();

 //ret = AP_protocol();

 //create timer
 esp_timer_create_args_t create_args;
 create_args.callback = timer_handle;
 create_args.arg = NULL;
 create_args.dispatch_method = ESP_TIMER_TASK;
 create_args.name = "timer\0";
 ret = esp_timer_create(&create_args, &(st.timer));
 ESP_ERROR_CHECK( ret );

 //setup_and_listen_promiscuous();
 setup_and_connect_wifi();
 while(st.status_value==ST_DISCONNECTED);

 acquire_server_ip();
 if(st.status_value==ST_GOT_IP)
 {
  printf("IP Acquired: %s\n", st.server_ip);
 }
 else
  esp_restart();

 connect_to_server();
 send_ready();
 recv_from_server();

 if(st.status_value != ST_SNIFFING)
  esp_restart();

 //setup promiscuous mode
 ESP_ERROR_CHECK(esp_wifi_set_promiscuous_rx_cb(event_handler_promiscuous));

 sniffer();
}

