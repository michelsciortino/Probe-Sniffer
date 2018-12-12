#ifndef H_UTIL
#define H_UTIL

void get_device_mac(char *mac);
void get_ssid(char *payload, int len, char *ssid);
void get_bssid(char *payload, char *bssid);
void print_raw_data(unsigned char *buf, int len);
void hash(char *source_mac, char *ssid, char *seq_num, char *timestamp, unsigned char *hash_str);
//void hash_packet(const BYTE *v, int length, BYTE *hash_str);
void get_seq_num(char *payload, char *seq_num);
struct timeval timeval_sub(struct timeval *a, struct timeval *b);
struct timeval timeval_add(struct timeval *a, struct timeval *b);
struct timeval timeval_durationToNow(struct timeval *start);
#endif  //H_UTIL
