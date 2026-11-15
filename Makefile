.PHONY: test test-gilligan test-skipper test-security test-howell-script

test:
	./packages/mary-ann-tests/scripts/test-all.sh

test-gilligan:
	./packages/mary-ann-tests/scripts/test-gilligan.sh

test-skipper:
	./packages/mary-ann-tests/scripts/test-skipper.sh

test-security:
	./packages/mary-ann-tests/scripts/test-security.sh

test-howell-script:
	./packages/mary-ann-tests/scripts/test-howell-script.sh
