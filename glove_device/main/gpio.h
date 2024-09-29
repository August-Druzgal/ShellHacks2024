#ifndef GPIO_H
#define GPIO_H

#include <driver/gpio.h>
#include <stdint.h>

#define PIN_MAX_MS 100
#define NUM_GPIOS 12
#define GPIO_HIGH 1
#define GPIO_LOW  0

struct gpio_pin_info {
    bool set;
    uint32_t counter;
    uint32_t frequency;
    bool ready;
};

void gpio_task_fn(void *args);
esp_err_t init_gpio_pin(int pin_num);
esp_err_t gpio_start_task(void);
esp_err_t gpio_init(void);
struct gpio_pin_info *get_pin_info(void);
esp_err_t gpio_set_pin_frequency(int pin_num, int pin_level);

#endif