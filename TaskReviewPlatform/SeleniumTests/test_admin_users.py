from time import sleep
import uuid
from selenium.webdriver.common.by import By
from selenium.webdriver.support import expected_conditions as EC


def test_admin_users_page_accessible_for_admin(
    driver, wait, base_url, login, admin_creds
):
    admin_login, admin_password = admin_creds
    login(admin_login, admin_password)
    driver.get(f"{base_url}/Admin/Users")

    wait.until(lambda d: "Список пользователей" in d.page_source)
    assert "Список пользователей" in driver.page_source
    


def test_admin_can_create_new_user(driver, wait, base_url, login, admin_creds):
    admin_login, admin_password = admin_creds
    login(admin_login, admin_password)

    driver.get(f"{base_url}/Admin/Users")

    login_value = "selenium_user_" + uuid.uuid4().hex[:5]

    # Для asp-for="InputUser.Login" Razor сгенерит id="InputUser_Login"
    login_input = wait.until(
        EC.visibility_of_element_located((By.ID, "InputUser_Login"))
    )
    name_input = driver.find_element(By.ID, "InputUser_Name")
    password_input = driver.find_element(By.ID, "InputUser_Password")

    login_input.clear()
    login_input.send_keys(login_value)

    name_input.clear()
    name_input.send_keys("Selenium Test User")

    password_input.clear()
    password_input.send_keys("SeleniumPass123!")

    form = login_input.find_element(By.XPATH, "./ancestor::form")
    submit = form.find_element(By.CSS_SELECTOR, "button[type='submit']")
    submit.click()

    wait.until(lambda d: login_value in d.page_source)
    assert login_value in driver.page_source
