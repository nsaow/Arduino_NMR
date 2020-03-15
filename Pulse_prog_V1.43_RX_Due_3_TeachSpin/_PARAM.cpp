#include <arduino.h>
#include "_PARAM.h"

/******************** AD9959 DDS control variables ********************/
double refFrequency = 1000000;                    // Initial frequency (1 MHz)
double ddsFreq[4] = {1000000, 1000000, 1000000, 1000000}; // Initial frequency (1 MHz)
unsigned long ddsAmp[4] = {500, 500, 500, 500};   // Initial amplitude; max 1023
double ddsPh[4] = {0,90,0,90};                    // Initial phase

/******************** Technical time parameters ********************/
const double Tmin = 2.00;                   // 2 us :: 21 clks  (stable)
const double Tsetup = Tmin;                 // us
const double TtrigWidth = Tmin;             // us
double Tdeadtime = 2;                       // us

/******************** Native USB data transfer ********************/
uint16_t FID_samples = 1000;                // 1000 points per FID  (default)
double T_transferF = 2000;                  // time that Arduino need to send out 1 FID (us) (not the PC receving time)
uint16_t Echo_samples = 10;                 // 20 points per echo (default)  
double T_transferE = 2000;                  // us

/******************** share PULSE SEQUENCE parameters ********************/
double Tp90 = Tmin;                         // us  default             
double Tp180 = 2*Tp90;                      // us  default                
double Ttr = 100000.0;                      // us (100 ms default)
double TreadoutF = 40000;                   // us (90 ms/FID default)
double TreadoutE = 500;                     // us (500 us/Echo default)
double Tres = 1000;                         // us (residue time)

/******************** sequence running ********************/
int runMode = 0;                            // 0:setup ; -1: data acquisition (repeat "nAvg" time)
boolean seqRunning = false;                 // indicated is a sequence is running
int nAvg = 1;                               // number of average
int nAcq = 0;                               // number of acquisition (run from 0 to (nAvg - 1) )
