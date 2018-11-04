#ifndef H_CONNECTION
#define H_CONNECTION

#include "codes.h"

void disconnect();
void reconnect();
int read_header(char *buf);
void recv_from_server();
void send_ready();
void acquire_server_ip();
esp_err_t event_handler_wifi(void *ctx, system_event_t *event);
void setup_and_connect_wifi(void);
void save_timestamp(char *buf);
void connect_to_server();
int Send(const void *data, size_t datalen, int flags);
void send_data();
void setup_AP();
void scan();
void setup_and_connect_wifi_old(void);

#endif
