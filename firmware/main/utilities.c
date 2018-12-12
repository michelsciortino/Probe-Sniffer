#include "utilities.h"
#include <string.h>
#include <sys/time.h>
#include "codes.h"

void get_device_mac(char *mac_str)
{
	uint8_t mac[6];
	int i;

	if (esp_wifi_get_mac(ESP_IF_WIFI_STA, mac) != ESP_OK)
	{
		esp_restart();
	}

	for (i = 0; i < 6; i++)
	{
		sprintf(mac_str + (i * 3), "%02x", mac[i]);
		if (i != 5)
			sprintf(mac_str + (i * 3) + 2, ":");
	}
}

void get_ssid(char *payload, int len, char *ssid)
{
	int pos = PROBE_PAYLOAD_POS;
	char c;
	int sublen;

	c = payload[pos];
	if (c != 0)
	{
		ssid[0] = '\0';
		return;
	}
	else
	{
		c = payload[pos + 1];
		sublen = (int)c;
		strncpy(ssid, payload + pos + 2, sublen);
		ssid[sublen] = '\0';
		return;
	}
}

void get_bssid(char *payload, char *bssid)
{
	strncpy(bssid, payload + PROBE_BSSID_POS, 6);
	bssid[6] = '\0';
}

void get_seq_num(char *payload, char *seq_num)
{
	strncpy(seq_num, payload + PROBE_SEQ_NUM_POS, 2);
	seq_num[2] = '\0';
}

//#HEX+ASCII PRINTER#
//print payload data in hex and ascii, for debug purposes
void print_raw_data(unsigned char *buf, int len)
{
	int i, j, c, fill_spaces;
	for (i = 0; i < len; i++)
	{
		printf("%02x ", buf[i] & 0xff);
		if (((i + 1) % 16) == 0 && i != 0)
		{
			printf("  |  ");
			for (j = 15; j >= 0; j--)
			{
				c = buf[i - j];
				if (isprint(c))
					printf("%c", (char)c);
				else
					printf(".");
			}
			printf("\n");
		}
		else if (((i + 1) % 8) == 0 && i != 0)
			printf("   ");
	}
	fill_spaces = 16 - (len % 16);
	if (fill_spaces >= 8)
		fill_spaces++;
	for (i = 0; i < fill_spaces; i++)
		printf("   ");

	printf("  |  ");
	for (j = (len % 16); j >= 0; j--)
	{
		c = buf[len - j];
		if (isprint(c))
			printf("%c", (char)c);
		else
			printf(".");
	}
	printf("\n\n");
}

void hash(char *source_mac, char *ssid, char *seq_num, char *timestamp, unsigned char *hash_str)
{
	unsigned char buff[MAC_LEN + SSID_MAXLEN + SEQ_NUM_LEN + TIME_LEN-3];
	int i=0;

	memcpy(buff+i, source_mac, MAC_LEN);
	i += MAC_LEN;

	memcpy(buff + i, ssid, SSID_MAXLEN);
	i += SSID_MAXLEN;
	
	memcpy(buff + i, seq_num, SEQ_NUM_LEN);
	i += SEQ_NUM_LEN;

	memcpy(buff +i, timestamp, TIME_LEN-3);
	i += TIME_LEN;

	SHA256_CTX ctx;
	sha256_init(&ctx);
	sha256_update(&ctx, buff, i);
	sha256_final(&ctx, hash_str);
}

struct timeval timeval_sub(struct timeval *a, struct timeval *b) {
	struct timeval result;
	result.tv_sec = a->tv_sec - b->tv_sec;
	result.tv_usec = a->tv_usec - b->tv_usec;
	if (a->tv_usec < b->tv_usec) {
		result.tv_sec -= 1;
		result.tv_usec += 1000000;
	}
	return result;
}

struct timeval timeval_add(struct timeval *a, struct timeval *b) {
	struct timeval result;
	result.tv_sec = a->tv_sec + b->tv_sec;
	result.tv_usec = a->tv_usec + b->tv_usec;
	if (result.tv_usec >= 1000000) {
		result.tv_sec += 1;
		result.tv_usec -= 1000000;
	}
	return result;
}

struct timeval timeval_durationToNow(struct timeval *start) {
	struct timeval b;
	gettimeofday(&b, NULL);
	struct timeval delta = timeval_sub(&b, start);
	return delta;
}