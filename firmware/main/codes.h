#ifndef H_CODES
#define H_CODES

#include <stdio.h> //DEBUG
#include <ctype.h> //DEBUG
#include <time.h>
#include <pthread.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "esp_system.h"
#include "esp_wifi.h"
#include "esp_event_loop.h"
#include "esp_pthread.h"
#include "nvs_flash.h"
#include "sys/socket.h"
#include "sha256.h"

/*Set the SSID and Password via "make menuconfig"*/
#define DEFAULT_SSID CONFIG_WIFI_SSID
#define DEFAULT_PWD CONFIG_WIFI_PASSWORD

//#define WEB_SERVER_FLAG CONFIG_WEB_SERVER_ONLY
//#define DEFAULT_SERVER_IP CONFIG_SERVER_IP

#define ESP_SSID "ESP_wifidirect"
#define ESP_PASSWORD "password"

#define DEFAULT_SCAN_METHOD WIFI_FAST_SCAN
#define DEFAULT_RSSI -127
#define DEFAULT_AUTHMODE WIFI_AUTH_OPEN
#define DEFAULT_SORT_METHOD WIFI_CONNECT_AP_BY_SIGNAL

#define BUFLEN 512
#define PORT 45445
#define SPORT 48448
#define LISTENQ 3
#define IPLEN 17

//ESP32 status codes
#define ST_DISCONNECTED 0
#define ST_CONNECTED 1
#define ST_GOT_IP 3
#define ST_ERR 4
#define ST_SNIFFING 5
#define ST_WAITING_TIME 6
#define ST_SENDING_DATA 7

#define HEADER_LEN 1
#define MAC_LEN 17
#define TIME_LEN 32
#define SRV_TIME_LEN 19
#define SSID_MAXLEN 32
#define TIMER_USEC 60000000
#define MAC_POS 10
#define STACK_SIZE 2000
#define SSID_LEN_POS 37
#define HASH_LEN 32
#define JSON_FIELD_LEN 69
#define JSON_HEAD_LEN 25+MAC_LEN
#define N_RECONNECT 5
#define JSON_MAC_POS 15
#define PAYLOAD_PROBE_POS 24 

#define AP_NOT_KING 0
#define AP_PRESENT 1
#define AP_KING 2

//codes received from server
#define CODE_OK 200
#define CODE_TIME 202
#define CODE_RESET 203
#define CODE_SRV_ADV 201
#define CODE_READY 100
#define CODE_DATA 101

struct packet_info
{
 char mac[MAC_LEN+1];
 char timestamp[TIME_LEN+13];
 char ssid[SSID_MAXLEN+1];
 char hash[(HASH_LEN*2)+1];
 int strength;
};

struct packet_node
{
 struct packet_info packet;
 struct packet_node *next;
};

struct status
{
 int status_value;
 char server_ip[IPLEN];
 int port;
 time_t srv_time;
 time_t client_time;
 struct packet_node *packet_list;
 uint16_t total_length;
 esp_timer_handle_t timer;
 bool king;
};

#endif
