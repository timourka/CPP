import sys
import pytest


def main():
    """
    Точка входа для запуска всех UI-тестов.
    Запускает pytest в текущей папке.
    """
    # -v            = подробный вывод
    # --maxfail=1   = остановиться после первого упавшего теста (можно убрать)
    args = ["-v", "--maxfail=2", "."]

    # pytest.main возвращает код выхода (0 если всё ок)
    result_code = pytest.main(args)
    sys.exit(result_code)


if __name__ == "__main__":
    main()
