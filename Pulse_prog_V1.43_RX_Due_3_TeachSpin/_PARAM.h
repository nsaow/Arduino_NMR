#include <arduino.h>

#ifndef _PARAM_H_
#define _PARAM_H_

/******************** AD9959 DDS control variables ***********************/
extern double refFrequency;
extern double ddsFreq[4];                
extern unsigned long ddsAmp[4];            
extern double ddsPh[4];                    

/******************** Technical time parameters ********************/
extern const double Tmin;
extern const double Tsetup;
extern const double TtrigWidth;
extern double Tdeadtime;

/******************** ADC variables ********************/
extern uint32_t samples;


/******************** Native USB data transfer ********************/
extern uint16_t FID_samples;     
extern double T_transferF;    
extern uint16_t Echo_samples;    
extern double T_transferE; 


/******************** share PULSE SEQUENCE parameters ********************/
extern double Tp90;
extern double Tp180;
extern double Ttr;
extern double TreadoutF;
extern double TreadoutE;
extern double Tres;

/******************** sequence running ********************/
extern int runMode;              
extern boolean seqRunning;
extern int nAvg;
extern int nAcq;  

#endif
