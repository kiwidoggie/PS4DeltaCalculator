#
#	PS4-Compare.py
#	By: kiwidog (kiwidog.me)
#	Used for delta'ing? changes in between PS4 Kernel Versions
#

from idautils import *
from idaapi import *
import sys
import idc
import os
import binascii

def Dump(p_FilePath):
	# Get the start EA from IDA
	s_Ea = BeginEA()

	# Open up our log file for writing
	s_File = open(p_FilePath, "w")

	# Shitty watermarking
	s_File.write("# Name, Start, End, Size, HexData")
	s_File.write("# Script by kiwidog")

	# Iterate through each function, skipping the default sub_***** ones
	for l_FuncEa in Functions(SegStart(s_Ea), SegEnd(s_Ea)):
		# Get the function name
		l_Name = GetFunctionName(l_FuncEa)
		
		# Get the start address of the function
		l_Start = l_FuncEa
		
		# Get the end address of the function
		l_End = FindFuncEnd(l_FuncEa)
		
		# Calculate size
		l_Size = l_End - l_Start
		
		# Grab the bytes needed
		l_Data = GetManyBytes(l_Start, l_Size);
		
		# Convert them to hex data
		l_HexData = binascii.hexlify(bytearray(l_Data))
		
		# Some formatting in order to reload later
		l_Output = "{}|{}|{}|{}|{}".format(l_Name, l_Start, l_End, l_Size, l_HexData)
		
		# Write it out to file
		s_File.write(l_Output + "\n")
		s_File.flush()

		# Some user logging
		print("Wrote: " + l_Name)

	# Finished
	print("Done!\n")
	
	# Close file handle
	s_File.close()
	
Dump("C:\\Users\\godiwik\\Desktop\\ps4-kernel-1.76-dump.txt")