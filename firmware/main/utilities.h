#ifndef H_UTIL
#define H_UTIL

void get_device_mac(char *mac);
void get_ssid(char *payload, int len, char *ssid);
void print_raw_data(unsigned char *buf, int len);

#endif  //H_UTIL
