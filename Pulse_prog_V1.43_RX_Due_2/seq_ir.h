#include <arduino.h>

#ifndef SEQ_IR_H_
#define SEQ_IR_H_

//******************** pulse sequence variable *******************//
extern double IRtau;
extern uint16_t nIR;

//******************** pulse sequence sub routine *******************//
int16_t compileIR();
#endif
