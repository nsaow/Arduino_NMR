#include <Arduino.h>
#include "_DUE_ADC.h"
#include "_SEND2PC.h"

/******************** public variables  ********************/
uint16_t DUE_ADC_I_buf[DUE_ADC_memDepth];       // I biffer
uint16_t DUE_ADC_Q_buf[DUE_ADC_memDepth];       // Q buffer 
double DUE_ADC_read_time = 20000;               // us (default 20 ms)
uint32_t DUE_ADC_samples = DUE_ADC_memDepth;    // data samples 
unsigned long buf_offset = 0;
unsigned long buf_segment = 1;
unsigned long total_samples = 1000;

/******************** private variables  ********************/
unsigned long DUE_ADC_count = round((double)DUE_ADC_read_time / DUE_ADC_min_sampling_time); // default 20000/9.23 = 2167 counts
double DUE_ADC_sampling_time = DUE_ADC_read_time / DUE_ADC_samples;                         // default 20000/2000 = 10 us
uint16_t DUE_ADC_sampling_ratio = DUE_ADC_count / DUE_ADC_samples;                          // default 2167/2000 = 1;

/******************** functions code ********************/
void DUE_ADC_Init(){
  pmc_enable_periph_clk(ID_ADC);
  adc_init(ADC, SystemCoreClock, 21000000UL, ADC_STARTUP_FAST);  // 1 Msps/ch :: 21Mhz clk or 500ksps / ch
  ADC->ADC_MR |= 0x80;      //set free running mode on ADC
  ADC->ADC_CR = 2;
  ADC->ADC_CHER = 0x81;     //enable ADC on pin A0 and A7 => b10000001  ************
}

void DUE_ADC_read(){
  unsigned long i;
  for( i = 0 ; i < DUE_ADC_count ; i++)                   //DUE_ADC_read_time in us
  {
    while((ADC->ADC_ISR & 0x81)!= 0x81);                  // wait for conversion ************                      
    DUE_ADC_I_buf[buf_offset + (i / DUE_ADC_sampling_ratio)] = ADC->ADC_CDR[7]; //get values from A0 -> I
    DUE_ADC_Q_buf[buf_offset + (i / DUE_ADC_sampling_ratio)] = ADC->ADC_CDR[0]; //get values from A7 -> Q ************
  }
  buf_offset = (buf_offset + DUE_ADC_samples) % total_samples;
}

int16_t DUE_ADC_setup(double readout_us, unsigned long segment, uint32_t samples)
{
  DUE_ADC_read_time = readout_us;           // us (integer)
  DUE_ADC_samples = samples;                // points
  DUE_ADC_count = round((double)DUE_ADC_read_time / DUE_ADC_min_sampling_time);
  DUE_ADC_sampling_time = DUE_ADC_read_time / DUE_ADC_samples;
  DUE_ADC_sampling_ratio = DUE_ADC_count / DUE_ADC_samples;
  buf_offset = 0;
  buf_segment = segment;
  total_samples = segment * samples;
  if(DUE_ADC_sampling_ratio < 1)            // (too small sampling time)               
    return(1);                              // error
  else
    return(0);                              // success
}

void reportADCStatus()
{
  sendText2PC("DUE_ADC_read_time = " + (String)DUE_ADC_read_time);
  sendText2PC("DUE_ADC_samples = " + (String)DUE_ADC_samples);
  //sendText2PC("DUE_ADC_count = " + (String)DUE_ADC_count);
  sendText2PC("DUE_ADC_sampling_time = " + (String)DUE_ADC_sampling_time);
  sendText2PC("DUE_ADC_sampling_ratio = " + (String)DUE_ADC_sampling_ratio);
  //sendText2PC("buf_offset = " + (String)buf_offset);
  sendText2PC("DUE_ADC_buf_segment = " + (String)buf_segment);
  sendText2PC("DUE_ADC_total_samples = " + (String)total_samples);
}
