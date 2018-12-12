#include "codes.h"
#include "led.h"
#include "driver/gpio.h"

void initialize_led(){
	//configuration for LED
	gpio_config_t io_conf;
	//disable interrupt
	io_conf.intr_type = GPIO_PIN_INTR_DISABLE;
	//set as output mode
	io_conf.mode = GPIO_MODE_OUTPUT;
	//bit mask of the pins you want to set
	io_conf.pin_bit_mask = BUILTIN_LED_PIN_BIT_MASK;
	//disable pull-down and pull-up mode
	io_conf.pull_down_en = 0;
	io_conf.pull_up_en = 0;
	//configure gpio in the given settings
	gpio_config(&io_conf);
}

void turn_led_on(){
	gpio_set_level(BUILTIN_LED_PIN, 1);
}

void turn_led_off(){
	gpio_set_level(BUILTIN_LED_PIN, 0);
}