#include <Arduino.h>

#ifndef _DUE_ADC_H_
#define _DUE_ADC_H_

/********************* public variables ******************************/
const double DUE_ADC_min_sampling_time = 2.00;      // min sampling time 2.00 us
const uint32_t DUE_ADC_memDepth = 10000;            // maximum buffer size (points)
extern uint16_t DUE_ADC_I_buf[DUE_ADC_memDepth];    // I buffer 
extern uint16_t DUE_ADC_Q_buf[DUE_ADC_memDepth];    // Q buffer 
extern double DUE_ADC_read_time;                    // total readout peroid (us) 
extern uint32_t DUE_ADC_samples;                    // number of data points
extern unsigned long buf_offset;
extern unsigned long buf_segment;
extern unsigned long total_samples; 

/********************* functions ************************************/
void DUE_ADC_Init();
void DUE_ADC_read();
int16_t DUE_ADC_setup(double readout_us, unsigned long segment, uint32_t samples);
void reportADCStatus();
#endif
