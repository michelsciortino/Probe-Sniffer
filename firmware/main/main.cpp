#include <iostream>

extern "C" {
 void app_main(void);
}

void app_main(void){
 std::cout << "Hello World\n\n";
}
