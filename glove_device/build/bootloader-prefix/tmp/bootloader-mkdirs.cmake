# Distributed under the OSI-approved BSD 3-Clause License.  See accompanying
# file Copyright.txt or https://cmake.org/licensing for details.

cmake_minimum_required(VERSION 3.5)

file(MAKE_DIRECTORY
  "C:/Users/August/esp/v5.3/esp-idf/components/bootloader/subproject"
  "C:/Users/August/Desktop/Shellhacks 2024/esp-idf/examples/bluetooth/bluedroid/classic_bt/bt_hid_mouse_device/build/bootloader"
  "C:/Users/August/Desktop/Shellhacks 2024/esp-idf/examples/bluetooth/bluedroid/classic_bt/bt_hid_mouse_device/build/bootloader-prefix"
  "C:/Users/August/Desktop/Shellhacks 2024/esp-idf/examples/bluetooth/bluedroid/classic_bt/bt_hid_mouse_device/build/bootloader-prefix/tmp"
  "C:/Users/August/Desktop/Shellhacks 2024/esp-idf/examples/bluetooth/bluedroid/classic_bt/bt_hid_mouse_device/build/bootloader-prefix/src/bootloader-stamp"
  "C:/Users/August/Desktop/Shellhacks 2024/esp-idf/examples/bluetooth/bluedroid/classic_bt/bt_hid_mouse_device/build/bootloader-prefix/src"
  "C:/Users/August/Desktop/Shellhacks 2024/esp-idf/examples/bluetooth/bluedroid/classic_bt/bt_hid_mouse_device/build/bootloader-prefix/src/bootloader-stamp"
)

set(configSubDirs )
foreach(subDir IN LISTS configSubDirs)
    file(MAKE_DIRECTORY "C:/Users/August/Desktop/Shellhacks 2024/esp-idf/examples/bluetooth/bluedroid/classic_bt/bt_hid_mouse_device/build/bootloader-prefix/src/bootloader-stamp/${subDir}")
endforeach()
if(cfgdir)
  file(MAKE_DIRECTORY "C:/Users/August/Desktop/Shellhacks 2024/esp-idf/examples/bluetooth/bluedroid/classic_bt/bt_hid_mouse_device/build/bootloader-prefix/src/bootloader-stamp${cfgdir}") # cfgdir has leading slash
endif()
