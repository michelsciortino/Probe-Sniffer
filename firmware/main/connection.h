#include <stdio.h> //DEBUG
#include <ctype.h> //DEBUG

#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "esp_system.h"
#include "esp_wifi.h"
#include "esp_event_loop.h"
#include "nvs_flash.h"
#include "sys/socket.h"

void disconnect();
void reconnect();
int read_header(char *buf);
void recv_from_server(int s);
void send_ready();
void acquire_server_ip();
static esp_err_t event_handler_wifi(void *ctx, system_event_t *event);
static void setup_and_connect_wifi(void);

