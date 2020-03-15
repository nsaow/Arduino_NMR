#include <arduino.h>

#ifndef SEQ_CALIB_H_
#define SEQ_CALIB_H_

//******************** pulse sequence variable *******************//
extern double Tpw0;
extern double Tdpw;
extern int npw;
extern int iq;

//******************** pulse sequence sub routine *******************//
int16_t compileCalib();
#endif
