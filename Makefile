.PHONY: test test-gilligan test-skipper test-security test-howell-script

test:
	./scripts/test-all.sh

test-gilligan:
	./scripts/test-gilligan.sh

test-skipper:
	./scripts/test-skipper.sh

test-security:
	./scripts/test-security.sh

test-howell-script:
	./scripts/test-howell-script.sh
