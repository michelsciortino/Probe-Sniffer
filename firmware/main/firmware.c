//main source file for the MalnatiProject firmware of ESP32

#include "codes.h"
#include "connection.h"
#include "sniffer.h"
#include "web_server.h"

#define SNIFFER_STACK_SIZE 2000

struct status st;

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

void print_spiff_file()
{
 esp_vfs_spiffs_conf_t conf = {
      .base_path = "/spiffs",
      .partition_label = NULL,
      .max_files = 5,
      .format_if_mount_failed = true,
    };

 esp_err_t ret = esp_vfs_spiffs_register(&conf);
 if (ret != ESP_OK)
 {
  printf("Error in SPIFFS\n");
 }

 size_t total = 0, used = 0;
 ret = esp_spiffs_info(NULL, &total, &used);
 if (ret != ESP_OK)
 {
  printf("Failed to get SPIFFS partition information (%s)", esp_err_to_name(ret));
 }
 else
 {
  printf("Partition size: total: %d, used: %d\n", total, used);
 }

 FILE* f;/* = fopen("/spiffs/test_file.txt", "w");
 if(f==NULL)
 {
  printf("Error opening file\n");
  return;
 }
 fprintf(f, "HelloWorld!!\n");
 fclose(f);*/
 f = fopen("/spiffs/test_file.txt", "r");
 int c;
 while((c = fgetc(f)) != EOF)
  printf("%c", (char)c);
 printf("\n");
 fclose(f);
}

void server_connection_task(void *pvParameter)
{
/*
 esp_err_t ret = nvs_flash_init();
 if (ret == ESP_ERR_NVS_NO_FREE_PAGES) {
  ESP_ERROR_CHECK(nvs_flash_erase());
  ret = nvs_flash_init();
 }
 ESP_ERROR_CHECK( ret );
*/

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

 //shuld not reach this point, if so connection with server is closed
 esp_restart();
}

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

/*
 initialize_st();
 print_spiff_file();
*/
#ifdef CONFIG_WEB_SERVER_ONLY
  web_server();
  return;
#endif

 //ret = AP_protocol();

 //create timer
 esp_timer_create_args_t create_args;
 create_args.callback = timer_handle;  //declared in sniffer.c:103
 create_args.arg = NULL;
 create_args.dispatch_method = ESP_TIMER_TASK;
 create_args.name = "timer\0";
 ret = esp_timer_create(&create_args, &(st.timer));
 ESP_ERROR_CHECK( ret );

 //setup_and_listen_promiscuous();
 xTaskCreate(&server_connection_task, "server_connection_task", 8192, NULL, 5, NULL);

 while(st.status_value != ST_SNIFFING) usleep(100);
  //esp_restart();

 wifi_init_config_t cfg = WIFI_INIT_CONFIG_DEFAULT();
 ESP_ERROR_CHECK(esp_wifi_init(&cfg));                  //TEMP

 //setup promiscuous mode
 ESP_ERROR_CHECK(esp_wifi_set_promiscuous_rx_cb(event_handler_promiscuous));


 ESP_ERROR_CHECK(esp_wifi_set_promiscuous(true));
 //sniffer();
}

