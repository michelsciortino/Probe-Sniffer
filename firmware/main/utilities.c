#include "utilities.h"
#include <string.h>
#include "codes.h"


void get_device_mac(char *mac_str)
{
 uint8_t mac[6];
 int i;

 if(esp_wifi_get_mac(ESP_IF_WIFI_STA, mac) != ESP_OK)
 {
  esp_restart();
 }

 for(i=0; i<6; i++)
 {
  sprintf(mac_str+(i*3), "%02x", mac[i]);
  if(i != 5)
   sprintf(mac_str+(i*3)+2, ":");
 }
}
