.DEFAULT_GOAL := build
.PHONY: build check format smoke world-harness world-performance world-pressure help

build:
	bash scripts/build.sh

check:
	bash scripts/check.sh

format:
	bash scripts/check.sh --fix

smoke: build
	bash scripts/launch.sh --burrito-smoke

world-harness:
	bash scripts/world-harness.sh

world-performance:
	bash scripts/world-harness.sh --performance

world-pressure:
	bash scripts/world-harness.sh --pressure

help:
	@echo 'make build  - build and install both DLLs into the local loader'
	@echo 'make check  - build with analyzers and verify formatting (no install)'
	@echo 'make format - apply formatting and remove unnecessary using directives'
	@echo 'make smoke  - build, launch the isolated game, then quit after 25 seconds'
	@echo 'make world-harness - run two native fixtures in a separate profile and save an evidence report'
	@echo 'make world-performance - run fixtures then nine automatic stationary trials (about 11 minutes)'
	@echo 'make world-pressure - run a capped native lifecycle burst and observe detail/recovery'
