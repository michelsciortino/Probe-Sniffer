#ifndef WEB_SERVER_H
#define WEB_SERVER_H

#include "esp_wifi.h"
#include "esp_event_loop.h"
#include <string.h>

// set AP CONFIG values
#ifdef CONFIG_AP_HIDE_SSID
 #define CONFIG_AP_SSID_HIDDEN 1
#else
 #define CONFIG_AP_SSID_HIDDEN 0
#endif
#ifdef CONFIG_WIFI_AUTH_OPEN
 #define CONFIG_AP_AUTHMODE WIFI_AUTH_OPEN
#endif
#ifdef CONFIG_WIFI_AUTH_WEP
 #define CONFIG_AP_AUTHMODE WIFI_AUTH_WEP
#endif
#ifdef CONFIG_WIFI_AUTH_WPA_PSK
 #define CONFIG_AP_AUTHMODE WIFI_AUTH_WPA_PSK
#endif
#ifdef CONFIG_WIFI_AUTH_WPA2_PSK
 #define CONFIG_AP_AUTHMODE WIFI_AUTH_WPA2_PSK
#endif
#ifdef CONFIG_WIFI_AUTH_WPA_WPA2_PSK
 #define CONFIG_AP_AUTHMODE WIFI_AUTH_WPA_WPA2_PSK
#endif
#ifdef CONFIG_WIFI_AUTH_WPA2_ENTERPRISE
 #define CONFIG_AP_AUTHMODE WIFI_AUTH_WPA2_ENTERPRISE
#endif

void web_server();
void configure_and_run_AP();
esp_err_t event_handler(void *ctx, system_event_t *event);
void web_conn_handler(void *pvParameter);

#endif
