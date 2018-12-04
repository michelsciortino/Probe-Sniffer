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
	// Create the semaphore to guard a shared resource.
	vSemaphoreCreateBinary(st.xSemaphore);
	st.status_value = ST_DISCONNECTED;
	strcpy(st.server_ip, "\0");
	st.port = -1;
	st.srv_time.tv_sec = 0;
	st.srv_time.tv_usec = 0;
	st.client_time.tv_sec = 0;
	st.client_time.tv_usec = 0;
	st.count = 0;
	st.total_length = 0;
	st.timer = NULL;
	st.king = false;
}

//main function
void app_main()
{
	TaskHandle_t xHandle = NULL;
	st.xSemaphore=NULL;
	printf("Booted\n");
	ESP_ERROR_CHECK(nvs_flash_erase());
	ESP_ERROR_CHECK(nvs_flash_init());
	//esp_log_level_set("*", ESP_LOG_ERROR);

	initialize_st();
	initialize_sniffer();
	initialize_led();

	printf("Connecting to Access Point...\n");
	setup_and_connect_wifi();
	while (st.status_value != ST_CONNECTED)
	{
		vTaskDelay(10);
	}
	printf("Connected.\n");
	printf("Waiting for server IP...\n");
	acquire_server_ip();
	if (st.status_value == ST_GOT_IP)
	{
		printf("Server IP acquired: %s\n", st.server_ip);
	}
	else
		esp_restart();

	printf("Connecting to server...\n");
	connect_to_server();
	send_ready();
	//recv_from_server();
	xTaskCreate(recv_from_server, "TASK_listener", 10000, NULL, 2, &xHandle);
	// Use the handle to delete the task.
	if (xHandle == NULL)
	{
		vTaskDelete(xHandle);
		esp_restart();
	}

	while (st.status_value != ST_READY){
		vTaskDelay(100);
	}
	//esp_restart();

	printf("Starting sniffing\n");
	start_sniffer();
}

