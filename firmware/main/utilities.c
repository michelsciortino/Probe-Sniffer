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

void get_ssid(char *payload, int len, char *ssid)
{
 int pos=PAYLOAD_PROBE_POS;
 char c;
 int sublen;

 c = payload[pos];
 if(c != 0)
 {
  ssid[0]='\0';
  return;
 }
 else
 {
  c = payload[pos+1];
  sublen = (int) c;
  strncpy(ssid, payload+pos+2, sublen);
  ssid[sublen]='\0';
  return;
 }

}

//#HEX+ASCII PRINTER#
//print payload data in hex and ascii, for debug purposes
void print_raw_data(unsigned char *buf, int len)
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

