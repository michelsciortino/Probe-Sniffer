#include "web_server.h"
#include "codes.h"
#include "utilities.h"

const static char http_html_hdr[] = "HTTP/1.1 200 OK\nContent-type: text/html\n\n";

void web_server()
{
 configure_and_run_AP();
 xTaskCreate( &web_conn_handler, "web_conn_handler", 8192, NULL, 5, NULL);
}

// AP event handler
esp_err_t event_handler(void *ctx, system_event_t *event)
{
 return ESP_OK;
}

void configure_and_run_AP()
{
 // initialize the tcp stack
 tcpip_adapter_init();

 // stop DHCP server
 ESP_ERROR_CHECK(tcpip_adapter_dhcps_stop(TCPIP_ADAPTER_IF_AP));

 // assign a static IP to the network interface
 tcpip_adapter_ip_info_t info;
 memset(&info, 0, sizeof(info));
 IP4_ADDR(&info.ip, 192, 168, 1, 1);
 IP4_ADDR(&info.gw, 192, 168, 1, 1);
 IP4_ADDR(&info.netmask, 255, 255, 255, 0);
 ESP_ERROR_CHECK(tcpip_adapter_set_ip_info(TCPIP_ADAPTER_IF_AP, &info));

 // start the DHCP server
 ESP_ERROR_CHECK(tcpip_adapter_dhcps_start(TCPIP_ADAPTER_IF_AP));

 // initialize the wifi event handler
 ESP_ERROR_CHECK(esp_event_loop_init(event_handler, NULL));

 // initialize the wifi stack in AccessPoint mode with config in RAM
 wifi_init_config_t wifi_init_config = WIFI_INIT_CONFIG_DEFAULT();
 ESP_ERROR_CHECK(esp_wifi_init(&wifi_init_config));
 ESP_ERROR_CHECK(esp_wifi_set_storage(WIFI_STORAGE_RAM));
 ESP_ERROR_CHECK(esp_wifi_set_mode(WIFI_MODE_AP));

 // configure the wifi connection and start the interface
 wifi_config_t ap_config = {
  .ap = {
   .ssid = CONFIG_AP_SSID,
   .password = CONFIG_AP_PASSWORD,
   .ssid_len = 0,
   .channel = CONFIG_AP_CHANNEL,
   .authmode = CONFIG_AP_AUTHMODE,
   .ssid_hidden = CONFIG_AP_SSID_HIDDEN,
   .max_connection = CONFIG_AP_MAX_CONNECTIONS,
   .beacon_interval = CONFIG_AP_BEACON_INTERVAL,
  },
 };
 ESP_ERROR_CHECK(esp_wifi_set_config(WIFI_IF_AP, &ap_config));

 // start the wifi interface
 ESP_ERROR_CHECK(esp_wifi_start());
}

//receive connections from a client (single client for now)
void web_conn_handler(void *pvParameter)
{
 int server_fd, client_fd, err;
 struct sockaddr_in server, client;
 char buf[BUFLEN];

 server_fd = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
 if (server_fd < 0)
  esp_restart();            //TO HANDLE

 server.sin_family = AF_INET;
 server.sin_port = htons(WEB_PORT);
 server.sin_addr.s_addr = htonl(INADDR_ANY);

 err = bind(server_fd, (struct sockaddr *) &server, sizeof(server));
 if (err < 0)
  esp_restart();             //TO HANDLE

 err = listen(server_fd, 2);
 if (err < 0)
  esp_restart();             //TO HANDLE

 while (1) {
  socklen_t client_len = sizeof(client);
  client_fd = accept(server_fd, (struct sockaddr *) &client, &client_len);

  printf("Client connected\n");

  if(client_fd < 0)
   continue;

  while (1)
  {
   int read = recv(client_fd, buf, BUFLEN, 0);
   printf("packet received:\n");
   if (read <= 0) break;

   //printf("packet received:\n");
   print_raw_data((unsigned char *)buf, read);

   char *request_line = strtok(buf, "\n");
   if(strstr(request_line, "GET "))
   {
    printf("GET received\n");
    sprintf(buf, "%s", http_html_hdr);
    strcat(buf, "<html><body><h1>Hello, World!</h1></body></html>");
    send(client_fd, buf, strlen(buf), 0);
    close(client_fd);
    break;
   }
  }
 }
}
