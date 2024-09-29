#set(COMPONENT_SRCS "main.c")
#set(COMPONENT_ADD_INCLUDEDIRS "")

#register_component()

idf_component_register(SRCS "main.c" "gpio.h"
                    INCLUDE_DIRS "."
                    REQUIRES bt nvs_flash esp_hid driver)
