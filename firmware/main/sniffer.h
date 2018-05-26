#ifndef H_SNIFFER
#define H_SNIFFER

#include "codes.h"

void event_handler_promiscuous(void *buf, wifi_promiscuous_pkt_type_t type);
void sniffer();
void clear_data();
void start_timer();
void timer_handle();
void print_data();
void calculate_timestamp(char *new_time);
void hash(const BYTE *v, int length, BYTE *hash_str);

#endif
