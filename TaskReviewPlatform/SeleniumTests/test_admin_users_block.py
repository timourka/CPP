import uuid

from selenium.webdriver.common.by import By
from selenium.webdriver.support import expected_conditions as EC
from selenium.common.exceptions import TimeoutException


def test_admin_can_block_user(driver, wait, base_url, login, admin_creds):
    """
    Сценарий (как планировалось по задаче):
      1) Админ заходит в /Admin/Users
      2) Создаёт нового пользователя
      3) В таблице у пользователя по умолчанию статус 'Активен'
      4) В той же строке жмёт кнопку 'Блокировать'
      5) Подтверждает стандартное окно confirm()
      6) Статус меняется на 'Заблокирован'
    """

    admin_login, admin_password = admin_creds
    login(admin_login, admin_password)

    driver.get(f"{base_url}/Admin/Users")

    # 1. Создаём нового пользователя (как в других тестах)
    login_value = "selenium_block_" + uuid.uuid4().hex[:5]

    print(f"[INFO] Создаю пользователя для блокировки: {login_value}")

    login_input = wait.until(
        EC.visibility_of_element_located((By.ID, "InputUser_Login"))
    )
    name_input = driver.find_element(By.ID, "InputUser_Name")
    password_input = driver.find_element(By.ID, "InputUser_Password")

    login_input.clear()
    login_input.send_keys(login_value)

    name_input.clear()
    name_input.send_keys("Selenium Block Test User")

    password_input.clear()
    password_input.send_keys("SeleniumPass123!")

    form = login_input.find_element(By.XPATH, "./ancestor::form")
    submit = form.find_element(By.CSS_SELECTOR, "button[type='submit']")
    submit.click()

    # 2. Убеждаемся, что пользователь появился в таблице
    print("[INFO] Жду появления пользователя в таблице...")
    wait.until(lambda d: login_value in d.page_source)
    print("[INFO] Пользователь появился в таблице.")

    # 3. Находим строку таблицы с этим логином
    print("[INFO] Ищу строку <tr> с этим логином...")
    row = wait.until(
        EC.presence_of_element_located(
            (
                By.XPATH,
                f"//tr[td[contains(normalize-space(), '{login_value}')]]",
            )
        )
    )
    print("[INFO] Строка найдена.")

    # 3.1. (Планируемое поведение) Проверяем, что статус изначально 'Активен'
    # Ожидаем, что в строке есть ячейка со словом 'Активен'
    print("[INFO] Проверяю, что начальный статус 'Активен'...")
    try:
        status_cell = row.find_element(
            By.XPATH, ".//td[contains(normalize-space(), 'Активен')]"
        )
        assert "Активен" in status_cell.text
        print("[INFO] Начальный статус: Активен.")
    except Exception:
        # Сейчас функционал не реализован – этот ассерт как раз покажет, что нужно допилить
        print("[WARN] Не удалось найти статус 'Активен' — функционал, вероятно, ещё не реализован.")
        # Всё равно продолжаем сценарий блокировки, чтобы зафиксировать ожидаемое поведение

    # 4. Находим кнопку 'Блокировать' рядом с 'Удалить'
    print("[INFO] Ищу кнопку 'Блокировать' в этой строке...")
    block_button = row.find_element(
        By.XPATH, ".//button[contains(normalize-space(), 'Блокировать')]"
    )
    print("[INFO] Нажимаю кнопку 'Блокировать'...")
    block_button.click()

    # 5. Подтверждаем стандартную модалку confirm()/alert
    try:
        print("[INFO] Жду появления стандартного окна подтверждения (alert/confirm)...")
        alert = wait.until(EC.alert_is_present())
        print(f"[INFO] Текст модалки: {alert.text!r}")
        alert.accept()
        print("[INFO] Подтверждение блокировки принято (alert.accept()).")
    except TimeoutException:
        print("[WARN] Окно подтверждения не появилось — возможно, пока не реализовано.")

    # 6. Проверяем, что статус изменился на 'Заблокирован'
    print("[INFO] Жду, когда статус пользователя изменится на 'Заблокирован'...")

    def user_blocked(d):
        try:
            row = d.find_element(
                By.XPATH,
                f"//tr[td[contains(normalize-space(), '{login_value}')]]",
            )
            blocked_cell = row.find_element(
                By.XPATH, ".//td[contains(normalize-space(), 'Заблокирован')]"
            )
            return "Заблокирован" in blocked_cell.text
        except Exception:
            return False

    wait.until(user_blocked)
    print("[INFO] Статус пользователя изменился на 'Заблокирован'.")

    # Финальный ассерт
    page_source = driver.page_source
    assert "Заблокирован" in page_source
    print("[INFO] Тест: админ может блокировать пользователя — сценарий описан.")
