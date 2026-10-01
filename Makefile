.DEFAULT_GOAL := build
.PHONY: build check format smoke help

build:
	bash scripts/build.sh

check:
	bash scripts/check.sh

format:
	bash scripts/check.sh --fix

smoke: build
	bash scripts/launch.sh --burrito-smoke

help:
	@echo 'make build  - build and install both DLLs into the local loader'
	@echo 'make check  - build with analyzers and verify formatting (no install)'
	@echo 'make format - apply formatting and remove unnecessary using directives'
	@echo 'make smoke  - build, launch the isolated game, then quit after 25 seconds'
