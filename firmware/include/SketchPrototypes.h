// Maintained derivative of koogar/Phat-Stats. Publication changes: LUSTgabriel, 2026-10-11.
// Original notices retained; see SOURCE_PROVENANCE.md and LICENSE.txt (GPL-2.0-or-later).
#pragma once

void Display_Port_Batt();
void splashScreen();
void button_Modes();
void FeatureSet_Indicator2();
void serialEvent();
void serialBTEvent();
void activityChecker();
void backlightON();
void backlightOFF();
void allNeoPixelsOff();
void allNeoPixelsRED();
void allNeoPixelsGREEN();
void allNeoPixelsBLUE();

void refreshNeopixels();
void CPU_loadGauge(int cpuUsage);
void CPU_tempGauge(int cpuDegree);
void GPU_loadGauge(int gpuUsage);
void GPU_tempGauge(int gpuDegree);

void PCB_TriggerCPU_load_Port(int cpuUsage);
void PCB_TriggerCPU_temp_Port(int cpuDegree);
void PCB_TriggerGPU_load_Port(int gpuUsage);
void PCB_TriggerGPU_temp_Port(int gpuDegree);

void updateCpuStatusIndicator(int cpuClockMhz, int cpuTempC);
void updateGpuStatusIndicator(int gpuClockMhz, int gpuTempC);
void resetStatusIndicators();

void drawCpuPixelArt(int x, int y);
void drawGpuPixelArt(int x, int y);
