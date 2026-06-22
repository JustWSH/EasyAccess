cmake_minimum_required(VERSION 3.25)
project(PrintWindows LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Console application
add_executable(PrintWindows
    main.cpp
)

target_link_libraries(PrintWindows PRIVATE
    user32
    kernel32
)
