#include <Arduino.h>
#include "_SEND2PC.h"

/*************************  variables  *********************************/
const uint8_t hl = 3;
uint8_t hbuf[2][hl] = { {0, 0, 0},   // text
                        {1, 0, 0}};  // data

double T_sendout_factor = 2.0;            // Sendout time of one IQ(4 Bytes) (4 bytes / 2 us; save approximation) (check!!!)
double T_PC_read_factor = 100.0;          // PC raed time of one IQ(4 Bytes) (4 bytes / 100 us; save approximation)                  
uint32_t Total_bytes = 2000;              // all data length in bytes
const uint16_t Max_Segment_bytes = 8000;  // max segment length 8000 bytes = 4000 points
//uint16_t Res_bytes = 0;                 // segment residue bytes
uint16_t n_segment = 1;                   // number of data segment
uint16_t segment_Bytes = 1000;            // segment length in bytes
uint16_t segment_sapmles = 500;           // segment length in bytes
  
/************************* functions code *******************************/
double get_transfer_time(uint16_t ndata, uint16_t samples)   
{ 
  n_segment = ndata;
  segment_Bytes = samples * 2;                  // 2 bytes per point
  segment_sapmles = samples;
  Total_bytes = n_segment * segment_Bytes;   
  return(2 * T_sendout_factor * (double)samples * (double)ndata); 
}  

void sendText2PC(String text)
{
  SerialUSB.write((uint8_t *) hbuf[0], hl);
  SerialUSB.println(text);
}

void sendData2PC(uint16_t I[], uint16_t Q[])
{
    hbuf[1][1] = segment_Bytes & 0xFF;            // low byte header
    hbuf[1][2] = (segment_Bytes >> 8) & 0xFF;     // high byte header
    for (int i = 0; i < n_segment; i++) 
    {
      SerialUSB.write((uint8_t *) hbuf[1], hl);
      SerialUSB.write((uint8_t *) &I[i*segment_sapmles], segment_Bytes);   // send I
      SerialUSB.write((uint8_t *) hbuf[1], hl);
      SerialUSB.write((uint8_t *) &Q[i*segment_sapmles], segment_Bytes);   // send Q
    }
}
