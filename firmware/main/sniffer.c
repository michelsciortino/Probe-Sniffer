void event_handler_promiscuous(void *buf, wifi_promiscuous_pkt_type_t type)
{
 int i;
 struct packet_node *new_node, *p;
 if(type != WIFI_PKT_MGMT)
  return;

 //to prevent creating nodes during sending process
 //if(st.status_value != ST_SNIFFING)
  //return;

 new_node = (struct packet_node *)malloc(sizeof(*new_node));

 //print mac address of the device
 for(i=0; i<6; i++)
 {
  sprintf(new_node->packet.mac+i*3, "%02x", ((wifi_promiscuous_pkt_t *)buf)->payload[MAC_POS+i]);
  //printf("%02x", ((wifi_promiscuous_pkt_t *)buf)->payload[MAC_POS+i]);
  if(i != 5)
   sprintf(new_node->packet.mac+2+i*3, ":");
   //printf(":");
 }
 //printf("\n");

 char ssid_len_c=((wifi_promiscuous_pkt_t *)buf)->payload[SSID_LEN_POS];
 int ssid_len=(int)ssid_len_c;
 printf("sender MAC: %s\n", new_node->packet.mac);
 printf("SSID length: %d\n", ssid_len);
 for(i=0; i<ssid_len; i++)
  printf("%c", ((wifi_promiscuous_pkt_t *)buf)->payload[SSID_LEN_POS+1+i]);
 printf("\n");

}

//sniffs packets then sends those to server in infinite loop
void sniffer()
{
 clear_data();
 start_timer();

 ESP_ERROR_CHECK(esp_wifi_set_promiscuous(true));
}


