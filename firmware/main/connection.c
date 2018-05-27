#include "connection.h"
#include "sniffer.h"
#include "utilities.h"

extern struct status st;
int srv_socket = 0;

void disconnect()
{
 ESP_ERROR_CHECK(esp_wifi_disconnect());
 ESP_ERROR_CHECK(esp_wifi_stop());
}

void reconnect()
{
 ESP_ERROR_CHECK(esp_wifi_start());
}

//returns code received in header from server
int read_header(char *buf)
{
 char head_val;
 sscanf(buf, "%c", &head_val);
 return (int)head_val;
}

//receive packets frome server and decides what to do
void recv_from_server()
{
 char buf[BUFLEN];
 int recv_len, code;

 recv_len=1;
 while(recv_len>0)
 {
  recv_len=recv(srv_socket, buf, BUFLEN, 0);
  if(recv_len <= 0)
  {
   close(srv_socket);
   connect_to_server();
   if(st.status_value != ST_ERR)
    recv_len=1;
   continue;
  }
  code=read_header(buf);
  switch(code)
  {
   case CODE_OK:
    st.status_value=ST_WAITING_TIME;
    break;
   case CODE_TIME:
    save_timestamp(buf);
    if(st.status_value != ST_SNIFFING)
    {
     st.status_value=ST_SNIFFING;
     return;
    }
    //start_timer();
    //start_sniffing();
    break;
   case CODE_RESET:
    close(srv_socket);
    esp_restart();
    break;
   default:
    close(srv_socket);
    esp_restart();
    break;
  }
 }
 close(srv_socket);
}

void connect_to_server()
{
 struct sockaddr_in str_sock_s;
 int result, i;
 struct in_addr in_addr;

 srv_socket=socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
 if(srv_socket<0)
 {
  st.status_value=ST_ERR;
  return;
 }
 memset(&str_sock_s, 0, sizeof(str_sock_s));
 str_sock_s.sin_family=AF_INET;
 str_sock_s.sin_port=htons(SPORT);
 if(inet_aton(st.server_ip, &in_addr)==0)
 {
  close(srv_socket);
  st.status_value=ST_ERR;
  return;
 }
 str_sock_s.sin_addr=in_addr;

 result = -1;
 for(i = 0; i < N_RECONNECT && result == -1; i++)
 {
  if(i != 0)
   sleep(1);
  result=connect(srv_socket, (struct sockaddr *)&str_sock_s, sizeof(str_sock_s));
 }
 if(result==-1)
 {
  close(srv_socket);
  printf("ERR failed to connect to server\n");
  esp_restart();
 }
}

void send_ready()
{
 int sent_len;
 char buf[BUFLEN];

 char c=CODE_READY;
 sprintf(buf, "%c", c);
 get_device_mac(buf+HEADER_LEN);

 sent_len=Send((const void *)buf, HEADER_LEN+MAC_LEN, 0);
 if(sent_len<0)
 {
  close(srv_socket);
  st.status_value=ST_ERR;
  return;
 }
 //recv_from_server(st.socket);
}

//receive the broadcast packet from server (UDP:45445) containing server ip address
void acquire_server_ip()
{
 int s, result, recv_len, i;
 struct sockaddr_in str_sock_s, str_sock_c;
 char buf[BUFLEN];
 unsigned int sockaddrlen;

 s=socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);

 if(s<0)
 {
  st.status_value=ST_ERR;
  return;
 }

 memset(&str_sock_s, 0, sizeof(str_sock_s));
 str_sock_s.sin_family=AF_INET;
 str_sock_s.sin_port=htons(PORT);
 str_sock_s.sin_addr.s_addr=htonl(INADDR_ANY);

 result=bind(s, (struct sockaddr *)&str_sock_s, sizeof(str_sock_s));
 if(result==-1)
 {
  close(s);
  return;
 }

 sockaddrlen=sizeof(struct sockaddr_in);

 while(1)
 {
  recv_len=recvfrom(s, buf, BUFLEN, 0, (struct sockaddr *)&str_sock_c, &sockaddrlen);
  if(recv_len > 0 && read_header(buf) == CODE_SRV_ADV)
  {
   for(i = 0; i < BUFLEN && (buf[i+1]!='\r' || buf[i+2]!='\n'); i++)
    st.server_ip[i] = buf[i+1];
   st.server_ip[i] = '\0';
   st.status_value=ST_GOT_IP;
   printf("Got server IP: %s\n", st.server_ip);
   close(s);
   break;
  }
 }
}

//handle wifi connection related events
esp_err_t event_handler_wifi(void *ctx, system_event_t *event)
{
 switch (event->event_id) {
  case SYSTEM_EVENT_STA_START:
   //ESP_LOGI(TAG, "SYSTEM_EVENT_STA_START");
   ESP_ERROR_CHECK(esp_wifi_connect());
   break;
  case SYSTEM_EVENT_STA_GOT_IP:
   //ESP_LOGI(TAG, "SYSTEM_EVENT_STA_GOT_IP");
   //ESP_LOGI(TAG, "Got IP: %s\n",ip4addr_ntoa(&event->event_info.got_ip.ip_info.ip));
   st.status_value=ST_CONNECTED;
   break;
  case SYSTEM_EVENT_STA_DISCONNECTED:
   //ESP_LOGI(TAG, "SYSTEM_EVENT_STA_DISCONNECTED");
   ESP_ERROR_CHECK(esp_wifi_connect());
   st.status_value=ST_DISCONNECTED;
   break;
  default:
   break;
 }
 return ESP_OK;
}

//does what the name says
void setup_and_connect_wifi(void)
{
 tcpip_adapter_init();
 ESP_ERROR_CHECK(esp_event_loop_init(event_handler_wifi, NULL));

 wifi_init_config_t cfg = WIFI_INIT_CONFIG_DEFAULT();
 ESP_ERROR_CHECK(esp_wifi_init(&cfg));
 wifi_config_t wifi_config = {
  .sta = {
   .ssid = DEFAULT_SSID,
   .password = DEFAULT_PWD,
  },
 };
 ESP_ERROR_CHECK(esp_wifi_set_mode(WIFI_MODE_STA));
 ESP_ERROR_CHECK(esp_wifi_set_config(ESP_IF_WIFI_STA, &wifi_config));
 ESP_ERROR_CHECK(esp_wifi_start());
}

//save time received from server and time on client when it arrives
void save_timestamp(char *buf)
{
 struct tm timestamp;
 char srv_time[TIME_LEN];
 int y, mon, d, h, min, sec;

 strncpy(srv_time, buf+2, TIME_LEN-2);
 srv_time[TIME_LEN-2]='\0';
 sscanf(srv_time, "%d:%d:%d:%d:%d:%d", &y, &mon, &d, &h, &min, &sec);
 timestamp.tm_year = y-1900;
 timestamp.tm_mon = mon;
 timestamp.tm_mday = d;
 timestamp.tm_hour = h;
 timestamp.tm_min = min;
 timestamp.tm_sec = sec;

 st.srv_time = mktime(&timestamp);

 st.client_time=time(NULL);
}

void send_data()
{
 char buf[BUFLEN];
 char header = CODE_DATA;
 struct packet_node *p;

 buf[0] = header;
 st.total_length += JSON_HEAD_LEN+2;
 buf[1] = (char) (st.total_length >> 8);
 buf[2] = (char) (st.total_length & 0xff);
 sprintf(buf+3, "{\"Esp_Mac\":\"");
 get_device_mac(buf+JSON_MAC_POS);
 sprintf(buf+JSON_MAC_POS+MAC_LEN, "\",\"Packets\":[");
 Send((const void *)buf, JSON_HEAD_LEN+3, 0);

 buf[0] = '\0';

 p = st.packet_list;
 while(p != NULL)
 {
  sprintf(buf, "{\"MAC\":\"%s\",\"SSID\":\"%s\",\"Timestamp\":\"%s\",\"Hash\":\"%s\",\"SignalStrength\":%04d,},", p->packet.mac, p->packet.ssid, p->packet.timestamp, p->packet.hash, p->packet.strength);
  Send(buf, strlen(buf), 0);
  p = p->next;
 }
 sprintf(buf, "]}");
 Send(buf, strlen(buf), 0);
}

int Send(const void *data, size_t datalen, int flags)
{
 int ret = send(srv_socket, data, datalen, flags);
 if(ret < 0)
 {
  close(srv_socket);
  connect_to_server();
  ret = send(srv_socket, data, datalen, flags);
 }

 return ret;
}
