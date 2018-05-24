#ifndef H_CONNECTION
#define H_CONNECTION

#include "codes.h"

void disconnect();
void reconnect();
int read_header(char *buf);
void recv_from_server(int s);
void send_ready();
void acquire_server_ip();
esp_err_t event_handler_wifi(void *ctx, system_event_t *event);
void setup_and_connect_wifi(void);
void save_timestamp(char *buf);

#endif
