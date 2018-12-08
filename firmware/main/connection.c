#include "connection.h"
#include "sniffer.h"
#include "utilities.h"

extern struct status st;
int srv_socket = 0;

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

int Send(const void *data, size_t datalen, int flags)
{
    int sent = 0, i;
    for (i = 0; i < 3 && sent == 0; i++)
    {
        int ret = send(srv_socket, data, datalen, flags);
        if (ret == datalen)
            sent = 1;
        if (ret == 0)
        {
            printf("Error: the socket is closed.\n");
            return -1;
        }
        if (ret < 0)
        {
            printf("\tError sending data... retrying\n");
        }
    }

    if (sent == 0)
    {
        printf("\tSend failed 3 times.. rebooting\n");
        close(srv_socket);
        return -1;
    }
    return datalen;
}

//receive packets frome server and decides what to do
void recv_from_server()
{
    char buf[BUFLEN];
    int recv_len, code;

    recv_len = 1;
    while (recv_len >= 0)
    {
        recv_len = read(srv_socket, buf, HEADER_LEN);
        if (recv_len <= 0)
        {
            if (recv_len == 0)
                printf("Connection error... The socket is closed. Reconnecting\n");
            else
                printf("Connection error... recvlen:%d. Reconnecting\n", recv_len);
            close(srv_socket);
            connect_to_server();
            send_ready();
            recv_len = 0;
            continue;
        }

        code = read_header(buf);
        switch (code)
        {
        case CODE_OK:
            st.status_value = ST_WAITING_TIME;
            printf("Received OK message from Server.\n");
            break;
        case CODE_TIME:
            recv_len = read(srv_socket, buf, SRV_TIME_LEN);
            if (recv_len <= 0)
                esp_restart();
            printf("Received TIMESTAMP message from Server.\n");
            save_timestamp(buf);
            if (st.status_value != ST_SNIFFING)
            {
                st.status_value = ST_READY;
                //  return;
            }
            break;
        case CODE_RESET:
            printf("Received RESET message from Server.\n");
            close(srv_socket);
            esp_restart();
            break;
        default:
            printf("Received UNKNOWN message from Server.\n");
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
    struct timeval tv;
    tv.tv_sec = READ_TIMEOUT;
    tv.tv_usec = 0;
    srv_socket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (srv_socket < 0)
    {
        st.status_value = ST_ERR;
        printf("Unable to create socket\n");
        ESP_ERROR_CHECK(ESP_FAIL);
    }
    memset(&str_sock_s, 0, sizeof(str_sock_s));
    str_sock_s.sin_family = AF_INET;
    str_sock_s.sin_port = htons(SPORT);
    if (inet_aton(st.server_ip, &in_addr) == 0)
    {
        close(srv_socket);
        printf("Unable to convert server ip from string\n");
        st.status_value = ST_ERR;
        ESP_ERROR_CHECK(ESP_FAIL);
    }
    str_sock_s.sin_addr = in_addr;
    setsockopt(srv_socket, SOL_SOCKET, SO_RCVTIMEO, (const char *)&tv, sizeof(tv));
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
        printf("Unable to connect to server\n");
        ESP_ERROR_CHECK(ESP_FAIL);
    }
    printf("Connected\n");
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
    printf("Ready Message sent\n");
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
        printf("Unable to create UDP listener socket\n");
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
        printf("Unable to bind UDP socket\n");
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
            close(s);
            break;
        }
    }
}

//handle wifi connection related events
esp_err_t event_handler(void *ctx, system_event_t *event)
{
    if (event->event_id == SYSTEM_EVENT_STA_GOT_IP) //The second event is SYSTEM_EVENT_STA_GOT_IP which indicates that we have been assigned an IP address by the DHCP server
    {
        //printf("GOT_IP EVENT FIRED\n");
        st.status_value = ST_CONNECTED;
    }
    if (event->event_id == SYSTEM_EVENT_STA_START)
    {
        //printf("STATION START EVENT FIRED\n");
        ESP_ERROR_CHECK(esp_wifi_connect());
    }
    if (event->event_id == SYSTEM_EVENT_STA_DISCONNECTED)
    {
        //printf("DISCONNECTED EVENT FIRED\n");
        ESP_ERROR_CHECK(esp_wifi_disconnect());
        st.status_value = ST_DISCONNECTED;
        ESP_ERROR_CHECK(esp_wifi_connect());
    }
    return ESP_OK;
}

void setup_and_connect_wifi(void)
{
    tcpip_adapter_init();
    ESP_ERROR_CHECK(esp_event_loop_init(event_handler, NULL));
    wifi_init_config_t cfg = WIFI_INIT_CONFIG_DEFAULT();
    ESP_ERROR_CHECK(esp_wifi_init(&cfg));
    wifi_config_t wifi_config = {
        .sta = {
            .ssid = DEFAULT_SSID,
            .password = DEFAULT_PWD,
            //.ssid_hidden = true,
        },
    };
    ESP_ERROR_CHECK(esp_wifi_set_mode(WIFI_MODE_APSTA));
    ESP_ERROR_CHECK(esp_wifi_set_config(ESP_IF_WIFI_STA, &wifi_config));
    ESP_ERROR_CHECK(esp_wifi_start());
}

//save time received from server and time on client when it arrives
void save_timestamp(char *buf)
{
    struct tm timestamp;
    char srv_time[SRV_TIME_LEN + 1];
    int y, mon, d, h, min, sec;
    long int usec;
    if (st.xSemaphore != NULL)
    {
        if (xSemaphoreGive(st.xSemaphore) != pdTRUE)
        {
            printf("Give Semaphore....\n");
            // We would expect this call to fail because we cannot give a semaphore without first "taking" it!
        }
        // Obtain the semaphore - don't block if the semaphore is not immediately available.
        if (xSemaphoreTake(st.xSemaphore, (TickType_t)0))
        {
            // We now have the semaphore and can access the shared resource.
            strncpy(srv_time, buf, SRV_TIME_LEN);
            srv_time[SRV_TIME_LEN] = '\0';
            printf("SERVER_TIME: %s\n", srv_time);
            sscanf(srv_time, "%d:%d:%d:%d:%d:%d:%ld", &y, &mon, &d, &h, &min, &sec, &usec);

            timestamp.tm_year = y - 1900;
            timestamp.tm_mon = mon - 1;
            timestamp.tm_mday = d;
            timestamp.tm_hour = h;
            timestamp.tm_min = min;
            timestamp.tm_sec = sec;
            // printf("Setting time: %s", asctime(&timestamp));

            st.srv_time.tv_sec = mktime(&timestamp);
            st.srv_time.tv_usec = usec;
            gettimeofday(&st.client_time, NULL);
            // We have finished accessing the shared resource so can free the semaphore.
            if (xSemaphoreGive(st.xSemaphore) != pdTRUE)
            {
                printf("ERROR Give.... \n");
                // We would not expect this call to fail because we must have obtained the semaphore to get here.
            }
        }
    }
}

int try_send_data()
{
    printf("Sending data\n");
    char buf[BUFLEN];
    char header = CODE_DATA;
    int ret;
    buf[0] = header;
    st.total_length += JSON_HEAD_LEN + 2;
    buf[1] = (char)(st.total_length >> 8);
    buf[2] = (char)(st.total_length & 0xff);

    sprintf(buf + 3, "{\"Esp_Mac\":\"");
    get_device_mac(buf + JSON_MAC_POS);
    sprintf(buf + JSON_MAC_POS + MAC_LEN, "\",\"Packets\":[");
    ret = Send((const void *)buf, JSON_HEAD_LEN + 3, 1);
    if (ret == -1)
        return -1;
    buf[0] = '\0';
    int i = 0;
    for (i = 0; i < st.count; i++)
    {
        sprintf(buf, "{\"MAC\":\"%s\",\"SSID\":\"%s\",\"Timestamp\":\"%s\",\"Seq_Num\":\"%09d\",\"SignalStrength\":%04d,},",
                st.packet_list[i % MAX_QUEUE_LEN].mac,
                st.packet_list[i % MAX_QUEUE_LEN].ssid,
                st.packet_list[i % MAX_QUEUE_LEN].timestamp,
                /*st.packet_list[i % MAX_QUEUE_LEN].hash,*/
                st.packet_list[i % MAX_QUEUE_LEN].seq_num,
                st.packet_list[i % MAX_QUEUE_LEN].strength);
        ret = Send(buf, strlen(buf), 1);
        if (ret == -1)
            return -1;
    }
    sprintf(buf, "]}");
    ret = Send(buf, strlen(buf), 0);
    if (ret == -1)
        return -1;
    printf("\tsent: %d packets\n", st.count);
    return 0;
}

void send_data()
{
    while (try_send_data() < 0)
    {
        close(srv_socket);
        connect_to_server();
    }
}
