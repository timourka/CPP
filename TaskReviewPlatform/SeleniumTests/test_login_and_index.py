from selenium.webdriver.common.by import By
from selenium.webdriver.support import expected_conditions as EC


def test_login_invalid_stays_on_login(driver, wait, base_url, login):
    login("wrong", "wrong")

    wait.until(EC.url_contains("/Login"))
    h2 = driver.find_element(By.TAG_NAME, "h2")
    assert h2.text == "Вход"


def test_login_valid_redirects_to_index(driver, wait, base_url, login, user_creds):
    user_login, user_password = user_creds
    login(user_login, user_password)

    # Index.cshtml: [Authorize], после логина редирект туда
    wait.until(
        lambda d: d.current_url.endswith("/Index")
        or d.current_url.rstrip("/") == base_url.rstrip("/")
    )

    h1 = driver.find_element(By.TAG_NAME, "h1")
    assert "Добро пожаловать" in h1.text


def test_index_for_user_shows_links(driver, wait, base_url, login, user_creds):
    user_login, user_password = user_creds
    login(user_login, user_password)

    wait.until(EC.presence_of_element_located((By.TAG_NAME, "ul")))
    page_source = driver.page_source

    assert "Список курсов" in page_source
    assert "Мои ответы" in page_source


def test_index_for_admin_shows_admin_link(driver, wait, base_url, login, admin_creds):
    admin_login, admin_password = admin_creds
    login(admin_login, admin_password)

    wait.until(EC.presence_of_element_located((By.TAG_NAME, "ul")))
    page_source = driver.page_source

    assert "Админ панель" in page_source
