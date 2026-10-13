.DEFAULT_GOAL := build
.PHONY: build check format smoke world-harness world-performance world-pressure world-advanced world-busy world-native world-food world-hydrant npc-probe npc-desires banana-quest help

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

world-advanced:
	bash scripts/world-harness.sh --advanced

world-busy:
	bash scripts/world-harness.sh --busy

world-native:
	bash scripts/world-harness.sh --native

world-food:
	bash scripts/world-harness.sh --food

world-hydrant:
	bash scripts/world-harness.sh --hydrant

npc-probe:
	bash scripts/world-harness.sh --npc-probe

banana-quest:
	bash scripts/world-harness.sh --banana-quest

npc-desires:
	bash scripts/world-harness.sh --npc-desires

help:
	@echo 'make build  - build and install both DLLs into the local loader'
	@echo 'make check  - build with analyzers and verify formatting (no install)'
	@echo 'make format - apply formatting and remove unnecessary using directives'
	@echo 'make smoke  - build, launch the isolated game, then quit after 25 seconds'
	@echo 'make world-harness - run two native fixtures in a separate profile and save an evidence report'
	@echo 'make world-performance - run fixtures then nine automatic stationary trials (about 11 minutes)'
	@echo 'make world-pressure - run a capped native lifecycle burst and observe detail/recovery'
	@echo 'make world-advanced - sustained native load, crowded detail and labeled writer backpressure'
	@echo 'make world-busy - nine comparable controlled actor/effect trials (about 11 minutes)'
	@echo 'make world-food - validate native dog sniff/eat/return and interruption cases'
	@echo 'make world-hydrant - validate dog approach/mark/return, bounded interruption and cleanup'
	@echo 'make world-native - food/hydrant/door metadata, API pause/resume and native scope reload'
	@echo 'make npc-probe - validate one civilian pickup, cancellation and native release'
	@echo 'make banana-quest - validate competing banana favors and lifecycle'
	@echo 'make npc-desires - validate autonomous food choices and interruptions'
