//main source file for the MalnatiProject firmware of ESP32

#include <stdio.h> //DEBUG
#include <ctype.h> //DEBUG
//#include "freertos/FreeRTOS.h"
//#include "freertos/task.h"
//#include "esp_system.h"
#include "esp_wifi.h"
//#include "esp_event_loop.h"
#include "nvs_flash.h"

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

void event_handler(void *buf, wifi_promiscuous_pkt_type_t type)
{
 printf("Packet received\n"); //DEBUG
 switch(type)
 {
  case WIFI_PKT_MGMT:
   printf("MANAGEMENT\n"); //DEBUG
   print_packet_info_mgmt((wifi_promiscuous_pkt_t *)buf);
   break;
  case WIFI_PKT_DATA:
   printf("DATA\n");
   print_data(((wifi_promiscuous_pkt_t *)buf)->payload, ((wifi_promiscuous_pkt_t *)buf)->rx_ctrl.sig_len);
   //print_packet_info((wifi_promiscuous_pkt_t *)buf);
   break;
  default:
   printf("OTHER PACKET\n"); //DEBUG
   break;
 }
}

int setup_and_listen_promiscuous()
{
 wifi_init_config_t cfg = WIFI_INIT_CONFIG_DEFAULT();

 // Initialize NVS
 esp_err_t ret = nvs_flash_init();
 if (ret == ESP_ERR_NVS_NO_FREE_PAGES) {
  ESP_ERROR_CHECK(nvs_flash_erase());
  ret = nvs_flash_init();
 }
 ESP_ERROR_CHECK( ret );

 ESP_ERROR_CHECK(esp_wifi_init(&cfg));
 ESP_ERROR_CHECK(esp_wifi_set_promiscuous_rx_cb(event_handler));
 ESP_ERROR_CHECK(esp_wifi_set_promiscuous(true));
 return 1;
}

void app_main()
{
 setup_and_listen_promiscuous();
 printf("*********************\n\n\n\n\n\n\n\n\n\n\n\n\n****************\n\n\n\n\n\n");
}
