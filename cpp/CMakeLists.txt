cmake_minimum_required(VERSION 3.20)
project(axon_parser VERSION 1.0.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

# Library
add_library(axon_parser
    src/axon_parser.cpp
)

target_include_directories(axon_parser
    PUBLIC
        $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
        $<INSTALL_INTERFACE:include>
)

# Example
add_executable(example
    example/main.cpp
)

target_link_libraries(example PRIVATE axon_parser)

# Compiler warnings
if(MSVC)
    target_compile_options(axon_parser PRIVATE /W4)
    target_compile_options(example PRIVATE /W4)
else()
    target_compile_options(axon_parser PRIVATE -Wall -Wextra -Wpedantic)
    target_compile_options(example PRIVATE -Wall -Wextra -Wpedantic)
endif()
