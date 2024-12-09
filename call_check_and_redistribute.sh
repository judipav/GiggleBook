#!/bin/bash

psql -d gigglebook -c "SELECT check_and_redistribute();"