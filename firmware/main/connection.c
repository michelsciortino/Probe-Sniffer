#include "connection.h"
#include "sniffer.h"
#include "utilities.h"

#define MAX_APS 30

extern struct status st;
int srv_socket = 0;
int flag_AP = 0;

void disconnect()
{
    ESP_ERROR_CHECK(esp_wifi_disconnect());
    ESP_ERROR_CHECK(esp_wifi_stop());
}

void reconnect()
{
    ESP_ERROR_CHECK(esp_wifi_start());
}

//returns code received in header from server
int read_header(char *buf)
{
    char head_val;
    sscanf(buf, "%c", &head_val);
    return (int)head_val;
}

//receive packets frome server and decides what to do
void recv_from_server()
{
    char buf[BUFLEN];
    int recv_len, code;

    recv_len = 1;
    while (recv_len > 0)
    {
        recv_len = recv(srv_socket, buf, BUFLEN, 0);
        if (recv_len <= 0)
        {
            close(srv_socket);
            connect_to_server();
            if (st.status_value != ST_ERR)
            {
                recv_len = 1;
                send_ready();
            }
            continue;
        }
        code = read_header(buf);
        printf("received %d header\n",code);
        switch (code)
        {
            case CODE_OK:
                st.status_value = ST_WAITING_TIME;
                break;
            case CODE_TIME:
                save_timestamp(buf);
                if (st.status_value != ST_SNIFFING)
                {
                    st.status_value = ST_READY;
                    return;
                }
                break;
            case CODE_RESET:
            default:
                close(srv_socket);
                esp_restart();
                break;
        }
    }
    close(srv_socket);
}

void connect_to_server()
{
    struct sockaddr_in str_sock_s;
    int result, i;
    struct in_addr in_addr;

    srv_socket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (srv_socket < 0)
    {
        st.status_value = ST_ERR;
        return;
    }
    memset(&str_sock_s, 0, sizeof(str_sock_s));
    str_sock_s.sin_family = AF_INET;
    str_sock_s.sin_port = htons(SPORT);
    if (inet_aton(st.server_ip, &in_addr) == 0)
    {
        close(srv_socket);
        st.status_value = ST_ERR;
        return;
    }
    str_sock_s.sin_addr = in_addr;

    result = -1;
    for (i = 0; i < N_RECONNECT && result == -1; i++)
    {
        if (i != 0)
            sleep(1);
        result = connect(srv_socket, (struct sockaddr *)&str_sock_s, sizeof(str_sock_s));
    }
    if (result == -1)
    {
        close(srv_socket);
        printf("ERR failed to connect to server\n");
        esp_restart();
    }
}

void send_ready()
{
    int sent_len;
    char buf[BUFLEN];

    char c = CODE_READY;
    sprintf(buf, "%c", c);
    get_device_mac(buf + HEADER_LEN);

    sent_len = Send((const void *)buf, HEADER_LEN + MAC_LEN, 0);
    if (sent_len < 0)
    {
        close(srv_socket);
        st.status_value = ST_ERR;
        return;
    }
    //recv_from_server(st.socket);
}

//receive the broadcast packet from server (UDP:45445) containing server ip address
void acquire_server_ip()
{
    int s, result, recv_len, i;
    struct sockaddr_in str_sock_s, str_sock_c;
    char buf[BUFLEN];
    unsigned int sockaddrlen;

    s = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);

    if (s < 0)
    {
        st.status_value = ST_ERR;
        return;
    }

    memset(&str_sock_s, 0, sizeof(str_sock_s));
    str_sock_s.sin_family = AF_INET;
    str_sock_s.sin_port = htons(PORT);
    str_sock_s.sin_addr.s_addr = htonl(INADDR_ANY);

    result = bind(s, (struct sockaddr *)&str_sock_s, sizeof(str_sock_s));
    if (result == -1)
    {
        st.status_value = ST_ERR;
        close(s);
        return;
    }

    sockaddrlen = sizeof(struct sockaddr_in);

    while (1)
    {
        recv_len = recvfrom(s, buf, BUFLEN, 0, (struct sockaddr *)&str_sock_c, &sockaddrlen);
        if (recv_len > 0 && read_header(buf) == CODE_SRV_ADV)
        {
            for (i = 0; i < BUFLEN && (buf[i + 1] != '\r' || buf[i + 2] != '\n'); i++)
                st.server_ip[i] = buf[i + 1];
            st.server_ip[i] = '\0';
            st.status_value = ST_GOT_IP;
            printf("Got server IP: %s\n", st.server_ip);
            close(s);
            break;
        }
    }
}

//handle wifi connection related events
esp_err_t event_handler(void *ctx, system_event_t *event)
{
    /*switch (event->event_id)
    {
    case SYSTEM_EVENT_STA_START:
        //ESP_LOGI(TAG, "SYSTEM_EVENT_STA_START");
        ESP_ERROR_CHECK(esp_wifi_connect());
        break;
    case SYSTEM_EVENT_STA_GOT_IP:
        //ESP_LOGI(TAG, "SYSTEM_EVENT_STA_GOT_IP");
        //ESP_LOGI(TAG, "Got IP: %s\n",ip4addr_ntoa(&event->event_info.got_ip.ip_info.ip));
        st.status_value = ST_CONNECTED;
        break;
    case SYSTEM_EVENT_STA_DISCONNECTED:
        //ESP_LOGI(TAG, "SYSTEM_EVENT_STA_DISCONNECTED");
        ESP_ERROR_CHECK(esp_wifi_connect());
        st.status_value = ST_DISCONNECTED;
        break;
    default:
        break;
    }
    return ESP_OK;*/

    if (event->event_id == SYSTEM_EVENT_SCAN_DONE)
    {
        //printf("Number of access points found: %d\n", event->event_info.scan_done.number);
        uint16_t apCount = event->event_info.scan_done.number;
        if (apCount == 0)
        {
            return ESP_OK;
        }
        wifi_ap_record_t list[MAX_APS];
        //wifi_ap_record_t *list =(wifi_ap_record_t *)malloc(sizeof(wifi_ap_record_t) * apCount);
        ESP_ERROR_CHECK(esp_wifi_scan_get_ap_records(&apCount, list));
        int i;
        for (i = 0; i < apCount; i++)
        {
            if (strcmp((char *)list[i].ssid, DEFAULT_SSID) == 0) //an ESP has already an AP
            {
                printf("%s found\n",DEFAULT_SSID); //DEBUG
                flag_AP = 1;
                break;
            }
        }
        if (flag_AP == 0)
            flag_AP = 2;
        ESP_ERROR_CHECK(esp_wifi_stop());
        // free(list);
    }
    if (event->event_id == SYSTEM_EVENT_AP_START)
    {
        st.status_value = ST_CONNECTED;
    }
    if (event->event_id == SYSTEM_EVENT_STA_CONNECTED) //The first is SYSTEM_EVENT_STA_CONNECTED indicating that we have connected to the access point.
    {
        //st.status_value = ST_CONNECTED;
    }
    if (event->event_id == SYSTEM_EVENT_STA_GOT_IP) //The second event is SYSTEM_EVENT_STA_GOT_IP which indicates that we have been assigned an IP address by the DHCP server
    {
        st.status_value = ST_CONNECTED;
    }
    return ESP_OK;
}

void setup_and_connect_wifi(void)
{
    wifi_config_t wifi_config = {
        .sta = {
            .ssid = DEFAULT_SSID,
            .password = DEFAULT_PWD,
        },
    };
    ESP_ERROR_CHECK(esp_wifi_set_config(ESP_IF_WIFI_STA, &wifi_config));
    ESP_ERROR_CHECK(esp_wifi_start());
    ESP_ERROR_CHECK(esp_wifi_connect());
}

bool does_AP_already_exist()
{
    tcpip_adapter_init();
    ESP_ERROR_CHECK(esp_event_loop_init(event_handler, NULL));
    wifi_init_config_t cfg = WIFI_INIT_CONFIG_DEFAULT();
    ESP_ERROR_CHECK(esp_wifi_init(&cfg));
    ESP_ERROR_CHECK(esp_wifi_set_storage(WIFI_STORAGE_RAM));
    ESP_ERROR_CHECK(esp_wifi_set_mode(WIFI_MODE_STA));
    ESP_ERROR_CHECK(esp_wifi_start());
    // Let us test a WiFi scan ...
    wifi_scan_config_t scanConf =
    {
        .ssid = NULL,
        .bssid = NULL,
        .channel = 0,
        .show_hidden = 1
    };
    ESP_ERROR_CHECK(esp_wifi_scan_start(&scanConf, 0));
    while (flag_AP == 0);
    if (flag_AP == 2)
    {
       return false;
    }
    else
    {
        return true;
    }
}

void setup_AP()
{
    printf("Setting up AP...\n");
    wifi_config_t wifi_config = {
        .ap = {
            .ssid = DEFAULT_SSID,
            .ssid_len = strlen(DEFAULT_SSID),
            .password = DEFAULT_PWD,
            .max_connection = 10, //TO CHANGE
            .authmode = WIFI_AUTH_WPA_WPA2_PSK,
        },
    };
    //if password is not set, set authentication to open
    if (strlen(DEFAULT_PWD) == 0)
    {
        wifi_config.ap.authmode = WIFI_AUTH_OPEN;
    }
    ESP_ERROR_CHECK(esp_wifi_set_mode(WIFI_MODE_AP));
    ESP_ERROR_CHECK(esp_wifi_set_config(ESP_IF_WIFI_AP, &wifi_config));
    ESP_ERROR_CHECK(esp_wifi_start());
}

//save time received from server and time on client when it arrives
void save_timestamp(char *buf)
{
    struct tm timestamp;
    char srv_time[SRV_TIME_LEN + 1];
    int y, mon, d, h, min, sec;

    strncpy(srv_time, buf + HEADER_LEN, SRV_TIME_LEN);
    srv_time[SRV_TIME_LEN] = '\0';
    printf("SERVER_TIME: %s\n", srv_time);
    sscanf(srv_time, "%d.%d.%d.%d.%d.%d", &y, &mon, &d, &h, &min, &sec);
    timestamp.tm_year = y - 1900;
    timestamp.tm_mon = mon;
    timestamp.tm_mday = d;
    timestamp.tm_hour = h;
    timestamp.tm_min = min;
    timestamp.tm_sec = sec;

    st.srv_time = mktime(&timestamp);
    st.client_time = time(NULL);
}

void send_data()
{
    printf("Sending data\n");
    char buf[BUFLEN];
    char header = CODE_DATA;

    buf[0] = header;
    st.total_length += JSON_HEAD_LEN+2;
    buf[1] = (char)(st.total_length >> 8);
    buf[2] = (char)(st.total_length & 0xff);
    sprintf(buf + 3, "{\"Esp_Mac\":\"");
    get_device_mac(buf + JSON_MAC_POS);
    sprintf(buf + JSON_MAC_POS + MAC_LEN, "\",\"Packets\":[");
    Send((const void *)buf, JSON_HEAD_LEN + 3, 0);
    buf[0] = '\0';
	
    int i = 0;
    for (i = 0; i < st.count; i++)
    {
        sprintf(buf, "{\"MAC\":\"%s\",\"SSID\":\"%s\",\"Timestamp\":\"%s\",\"Hash\":\"%s\",\"SignalStrength\":%04d,},",
                st.packet_list[i % MAX_QUEUE_LEN].mac,
                st.packet_list[i % MAX_QUEUE_LEN].ssid,
                st.packet_list[i % MAX_QUEUE_LEN].timestamp,
                st.packet_list[i % MAX_QUEUE_LEN].hash,
                st.packet_list[i % MAX_QUEUE_LEN].strength);
        Send(buf, strlen(buf), 0);
    }
    sprintf(buf, "]}");
    Send(buf, strlen(buf), 0);
    printf("Data sent\n\tsent: %d packets\n",st.count);
}

int Send(const void *data, size_t datalen, int flags)
{
    int ret = send(srv_socket, data, datalen, flags);
    if (ret < 0)
    {
        printf("\tError sending data... retrying\n");
        close(srv_socket);
        connect_to_server();
        ret = send(srv_socket, data, datalen, flags);
    }

    if(ret<0){
        esp_restart();
    }

    return ret;
}
