//main source file for the MalnatiProject firmware of ESP32

#include "codes.h"
#include "connection.h"
#include "sniffer.h"
#include "led.h"
#include "esp_log.h"

#define SNIFFER_STACK_SIZE 2000

struct status st;

//initialize the main structure
void initialize_st()
{
	st.status_value = ST_DISCONNECTED;
	strcpy(st.server_ip, "\0");
	st.port = -1;
	st.srv_time = 0;
	st.client_time = 0;
	st.count = 0;
	st.total_length = 0;
	st.timer = NULL;
	st.king = false;
}

//main function
void app_main()
{
	ESP_ERROR_CHECK(nvs_flash_erase());
	ESP_ERROR_CHECK(nvs_flash_init());
	esp_log_level_set("*", ESP_LOG_ERROR);

	initialize_st();
	initialize_sniffer();
	initialize_led();

	if(does_AP_already_exist()==false){
		setup_AP();
		printf("Access Point Created\n");
	}
	else
		setup_and_connect_wifi();
	while (st.status_value != ST_CONNECTED);
	printf("Connected to Access Point\n");

	acquire_server_ip();
	if (st.status_value == ST_GOT_IP)
	{
		printf("Server IP Acquired: %s\n", st.server_ip);
	}
	else
		esp_restart();

	connect_to_server();
	if (st.status_value == ST_ERR)
	{
		printf("Error connecting to server\n");
		esp_restart();
	}
	send_ready();
	recv_from_server();

	if (st.status_value != ST_READY)
		esp_restart();

	printf("Starting sniffing\n");
	start_sniffer();
}
