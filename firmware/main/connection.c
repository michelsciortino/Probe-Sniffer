#include "connection.h"
#include "codes.h"

extern struct status st;

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
void recv_from_server(int s)
{
 char buf[BUFLEN];
 int recv_len, code;

 recv_len=1;
 while(recv_len>0)
 {
  recv_len=recv(s, buf, BUFLEN, 0);
  code=read_header(buf);
  switch(code)
  {
   case CODE_OK:
    st.status_value=ST_WAITING_TIME;
    break;
   case CODE_TIME:
    st.status_value=ST_SNIFFING;
    save_timestamp(buf);
    //start_timer();
    //start_sniffing();
    break;
   case CODE_RESET:
    close(s);
    esp_restart();
    break;
   default:
    close(s);
    esp_restart();
    break;
  }
 }
 close(s);
}

void send_ready()
{
 int s, result, i, sent_len;
 struct sockaddr_in str_sock_s;
 char buf[BUFLEN];
 //unsigned int sockaddrlen;
 struct in_addr in_addr;
 uint8_t mac[6];

 s=socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);

 if(s<0)
 {
  st.status_value=ST_ERR;
  return;
 }

 memset(&str_sock_s, 0, sizeof(str_sock_s));
 str_sock_s.sin_family=AF_INET;
 str_sock_s.sin_port=htons(SPORT);
 if(inet_aton(st.server_ip, &in_addr)==0)
 {
  close(s);
  st.status_value=ST_ERR;
  return;
 }
 str_sock_s.sin_addr=in_addr;

 //printf("Trying to connect to %s:%d\n", inet_ntoa(str_sock_s.sin_addr), ntohs(str_sock_s.sin_port));

 result=connect(s, (struct sockaddr *)&str_sock_s, sizeof(str_sock_s));
 if(result==-1)
 {
  close(s);
  st.status_value=ST_ERR;
  return;
 }

 if(esp_wifi_get_mac(ESP_IF_WIFI_STA, mac) != ESP_OK)
 {
  close(s);
  st.status_value=ST_ERR;
  return;
 }

 char c=CODE_READY;
 sprintf(buf, "%c", c);
 for(i=0; i<6; i++)
  sprintf(buf+HEADER_LEN+(i*3), "%02x:", mac[i]);
 buf[9+(i*3)]='\0';

 sent_len=send(s, buf, HEADER_LEN+MAC_LEN, 0);
 if(sent_len<0)
 {
  close(s);
  st.status_value=ST_ERR;
  return;
 }
 recv_from_server(s);
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
  if(strncmp(buf, "server:", 7)==0)
  {
   for(i=0; i<recv_len-7 && buf[7+i]!=':'; i++)
   st.server_ip[i]=buf[7+i];
   st.server_ip[i]='\0';
   st.status_value=ST_GOT_IP;
   close(s);
   break;
  }
 }
}

//handle wifi connection related events
static esp_err_t event_handler_wifi(void *ctx, system_event_t *event)
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
static void setup_and_connect_wifi(void)
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
