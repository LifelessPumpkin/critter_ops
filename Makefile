.PHONY: test test-gilligan test-skipper

test:
	./packages/mary-ann-tests/scripts/test-all.sh

test-gilligan:
	./packages/mary-ann-tests/scripts/test-gilligan.sh

test-skipper:
	./packages/mary-ann-tests/scripts/test-skipper.sh
