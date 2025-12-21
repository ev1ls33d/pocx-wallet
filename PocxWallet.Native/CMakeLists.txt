cmake_minimum_required(VERSION 3.18)
project(PocxWalletNative LANGUAGES CXX)

# Set C++ standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Option to enable CUDA
option(ENABLE_CUDA "Enable CUDA support" ON)

# Source files
set(SOURCES
    src/vanitysearch_wrapper.cpp
)

# Try to find CUDA
if(ENABLE_CUDA)
    find_package(CUDA QUIET)
    if(CUDA_FOUND)
        message(STATUS "CUDA found: ${CUDA_VERSION}")
        enable_language(CUDA)
        set(CMAKE_CUDA_STANDARD 17)
        # Add CUDA sources here when integrating VanitySearch-PocX
        # set(SOURCES ${SOURCES} src/gpu_vanity.cu)
    else()
        message(WARNING "CUDA not found, building CPU-only version")
    endif()
endif()

# Create shared library
add_library(pocxwallet_native SHARED ${SOURCES})

# Set output directory
set_target_properties(pocxwallet_native PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib"
)

# Include directories
target_include_directories(pocxwallet_native PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/include
)

# Link CUDA libraries if available
if(CUDA_FOUND)
    target_link_libraries(pocxwallet_native ${CUDA_LIBRARIES})
    target_include_directories(pocxwallet_native PRIVATE ${CUDA_INCLUDE_DIRS})
endif()

# Installation
install(TARGETS pocxwallet_native
    LIBRARY DESTINATION lib
    RUNTIME DESTINATION bin
)

install(FILES include/vanitysearch_wrapper.h
    DESTINATION include
)
