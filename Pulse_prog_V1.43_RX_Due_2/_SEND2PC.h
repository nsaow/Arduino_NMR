#include <Arduino.h>

#ifndef _SEND2PC_H_
#define _SEND2PC_H_

//extern double T_sendout_factor;
//extern double T_PC_read_factor;
//extern uint16_t Data_bytes;

/********************* functions ************************************/
double get_transfer_time(uint16_t ndata, uint16_t samples);
void sendText2PC(String text);
void sendData2PC(uint16_t bI[], uint16_t bQ[]);
#endif
