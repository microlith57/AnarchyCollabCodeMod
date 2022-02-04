#!/bin/bash

CELESTEGAMEPATH=$PWD/../.. dotnet build -c Release
zip AnarchyCollab.zip -r everest.yaml AnarchyCollab.dll Ahorn
