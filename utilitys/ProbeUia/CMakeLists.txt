cmake_minimum_required(VERSION 3.25)
project(ProbeUia LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_WIN32_EXECUTABLE OFF)

add_executable(ProbeUia main.cpp)
target_link_libraries(ProbeUia PRIVATE uiautomationcore ole32)
