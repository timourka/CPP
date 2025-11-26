import uuid
from selenium.common.exceptions import TimeoutException
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.common.by import By


def test_admin_can_delete_user(driver, wait, base_url, login, admin_creds):
    """
    Админ:
      1) заходит в /Admin/Users
      2) создаёт нового пользователя
      3) находит его в таблице
      4) удаляет
      5) проверяет, что в таблице его больше нет
    """
    admin_login, admin_password = admin_creds
    login(admin_login, admin_password)

    driver.get(f"{base_url}/Admin/Users")

    # 1. Создаём нового пользователя (как в тесте на создание)
    login_value = "selenium_del_" + uuid.uuid4().hex[:5]

    login_input = wait.until(
        EC.visibility_of_element_located((By.ID, "InputUser_Login"))
    )
    name_input = driver.find_element(By.ID, "InputUser_Name")
    password_input = driver.find_element(By.ID, "InputUser_Password")

    login_input.clear()
    login_input.send_keys(login_value)

    name_input.clear()
    name_input.send_keys("Selenium Delete Test User")

    password_input.clear()
    password_input.send_keys("SeleniumPass123!")

    form = login_input.find_element(By.XPATH, "./ancestor::form")
    submit = form.find_element(By.CSS_SELECTOR, "button[type='submit']")
    submit.click()

     # Убеждаемся, что пользователь появился в таблице
    print(f"[INFO] Жду появления пользователя {login_value} в таблице...")
    wait.until(lambda d: login_value in d.page_source)
    print(f"[INFO] Пользователь {login_value} найден в таблице.")

    # 2. Находим строку таблицы с этим логином
    print(f"[INFO] Ищу строку <tr> с логином {login_value}...")
    row = wait.until(
        EC.presence_of_element_located(
            (
                By.XPATH,
                f"//tr[td[contains(normalize-space(), '{login_value}')]]",
            )
        )
    )
    print("[INFO] Строка с пользователем найдена.")

    # 3. Жмём кнопку "Удалить" в этой строке
    print("[INFO] Ищу кнопку 'Удалить' в этой строке...")
    delete_button = row.find_element(
        By.XPATH, ".//button[contains(normalize-space(), 'Удалить')]"
    )
    print("[INFO] Нажимаю кнопку 'Удалить'...")
    delete_button.click()

    # 3.1. Подтверждаем стандартную модалку браузера (JS confirm / alert)
    try:
        print("[INFO] Жду появления стандартного окна подтверждения (alert/confirm)...")
        alert = wait.until(EC.alert_is_present())
        print(f"[INFO] Текст модалки: {alert.text!r}")
        alert.accept()
        print("[INFO] Подтверждение принято (alert.accept()).")
    except TimeoutException:
        print("[WARN] Стандартное окно подтверждения не появилось.")

    # 4. Проверяем, что логина больше нет в таблице
    print(f"[INFO] Жду, когда пользователь {login_value} исчезнет из таблицы...")
    wait.until(lambda d: login_value not in d.page_source)
    print(f"[INFO] Пользователь {login_value} больше не найден в таблице.")
    assert login_value not in driver.page_source
    print("[INFO] Проверка удаления пользователя прошла успешно.")
