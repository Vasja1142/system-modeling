#include <iostream>
#include <vector>
#include <random>
#include <cmath>
#include <fstream>
#include <iomanip>

using namespace std;

double LAMBDA = 9;
double MU1 = 6;
double MU2 = 3;
double N = 1000;

struct car {
    double timeArrival;

    double ko1 = 0;
    double ko2 = 0;
    double queue = 0;

    bool isCancelled() {
        if (ko1 == 0 && ko2 == 0) {
            return true;
        }
        return false;
    }

    double timeDeparture() {
        return timeArrival + ko1 + ko2 + queue;
    }
};

vector<double> z;
vector<pair<double, double>> ko1;
vector<pair<double, double>> ko2;
vector<pair<double, double>> mo;
vector<car> cars;

double generate_tau() {
    int value = rand() % 100;
    double r = (double)value / 100;
    if (r < 0.0001) {
        r = 0.01;
    }
    r = -1 / LAMBDA * log(r);
    r *= 100;
    r = round(r);
    r = r / 100;
    return r;
}

double generate_mu1() {
    int value = rand() % 100;
    double r = (double)value / 100;
    if (r < 0.0001) {
        r = 0.01;
    }
    r = -1 / MU1 * log(r);
    r *= 100;
    r = round(r);
    r = r / 100;
    return r;
}

double generate_mu2() {
    int value = rand() % 100;
    double r = (double)value / 100;
    if (r < 0.0001) {
        r = 0.01;
    }
    r = -1 / MU2 * log(r);
    r *= 100;
    r = round(r);
    r = r / 100;
    return r;
}

void generate_z() {
    z.push_back(generate_tau());
    for (int i = 1; i < N; i++) {
        z.push_back(generate_tau() + z[i - 1]);
    }
}

void obrab() {
    for (int i = 0; i < z.size(); i++) {
        car c;
        c.timeArrival = z[i];

        double ko1Value = generate_mu1();
        double ko2Value = generate_mu2();

        if (ko1.size() == 0) {
            ko1.push_back(make_pair(z[i], z[i] + ko1Value));
            c.ko1 = ko1Value;
            cars.push_back(c);
            continue;
        }
        if (ko1.size() != 0 && ko1[ko1.size() - 1].second <= z[i]) {
            ko1.push_back(make_pair(z[i], z[i] + ko1Value));
            c.ko1 = ko1Value;
            cars.push_back(c);
            continue;
        }
        if (ko2.size() == 0) {
            ko2.push_back(make_pair(z[i], z[i] + ko2Value));
            c.ko2 = ko2Value;
            cars.push_back(c);
            continue;
        }
        if (ko2.size() != 0 && ko2[ko2.size() - 1].second <= z[i]) {
            ko2.push_back(make_pair(z[i], z[i] + ko2Value));
            c.ko2 = ko2Value;
            cars.push_back(c);
            continue;
        }

        double ko1last = ko1[ko1.size() - 1].second;
        double ko2last = ko2[ko2.size() - 1].second;

        if (mo.size() == 0) {
            if (ko1last <= ko2last) {
                mo.push_back(make_pair(z[i], ko1last));
                ko1.push_back(make_pair(ko1last, ko1last + ko1Value));
                c.ko1 = ko1Value;
                c.queue = mo[mo.size() - 1].second - mo[mo.size() - 1].first;
                cars.push_back(c);
                continue;
            }
            if (ko1last > ko2last) {
                mo.push_back(make_pair(z[i], ko2last));
                ko2.push_back(make_pair(ko2last, ko2last + ko2Value));
                c.ko2 = ko2Value;
                c.queue = mo[mo.size() - 1].second - mo[mo.size() - 1].first;
                cars.push_back(c);
                continue;
            }
        }

        if (mo.size() != 0 && mo[mo.size() - 1].second <= z[i]) {
            if (ko1last <= ko2last) {
                mo.push_back(make_pair(z[i], ko1last));
                ko1.push_back(make_pair(ko1last, ko1last + ko1Value));
                c.queue = mo[mo.size() - 1].second - mo[mo.size() - 1].first;
                c.ko1 = ko1Value;
                cars.push_back(c);
                continue;
            }
            if (ko1last > ko2last) {
                mo.push_back(make_pair(z[i], ko2last));
                ko2.push_back(make_pair(ko2last, ko2last + ko2Value));
                c.queue = mo[mo.size() - 1].second - mo[mo.size() - 1].first;
                c.ko2 = ko2Value;
                cars.push_back(c);
                continue;
            }
        }

        cars.push_back(c);
        continue;
    }
}

double time_K2_and_K1_are_not_free() {
    double totalOverlapTime = 0;

    if (ko1.empty() || ko2.empty()) return 0;

    int i = 0, j = 0;

    while (i < ko1.size() && j < ko2.size()) {
        double overlapStart = max(ko1[i].first, ko2[j].first);
        double overlapEnd = min(ko1[i].second, ko2[j].second);

        if (overlapStart < overlapEnd) {
            totalOverlapTime += (overlapEnd - overlapStart);
        }

        if (ko1[i].second < ko2[j].second) {
            i++;
        }
        else if (ko2[j].second < ko1[i].second) {
            j++;
        }
        else {
            i++;
            j++;
        }
    }
    return totalOverlapTime;
}

void computeValues() {
    double time = cars[N - 1].timeDeparture() - cars[0].timeArrival;
    int cancelled = 0;
    int serviced = 0;
    double countTimeko1 = 0;
    double countTimeko2 = 0;
    double totalQueueTime = 0;
    int queuecount = 0;

    for (int i = 0; i < N; i++) {
        if (cars[i].isCancelled()) {
            cancelled++;
        }
        else {
            serviced++;
        }

        countTimeko1 += cars[i].ko1;
        countTimeko2 += cars[i].ko2;
        totalQueueTime += cars[i].queue;

        if (cars[i].queue != 0) {
            queuecount++;
        }
    }

    double veronebusy = (countTimeko1 + countTimeko2 - 2 * time_K2_and_K1_are_not_free()) / time;

    cout << "========== ОСНОВНЫЕ ПОКАЗАТЕЛИ ==========" << endl;
    cout << "Обслужено: " << serviced << endl;
    cout << "Отказов: " << cancelled << endl;
    cout << "Всего заявок: " << serviced + cancelled << endl;
    cout << "Время моделирования: " << time << endl;
    cout << "Вероятность занятости первой колонки: " << countTimeko1 / time << endl;
    cout << "Вероятность занятости второй колонки: " << countTimeko2 / time << endl;
    cout << endl;

    cout << "========== ВЕРОЯТНОСТНЫЕ ХАРАКТЕРИСТИКИ ==========" << endl;
    cout << "Пропускная способность = " << serviced / time << endl;
    cout << "Вероятность обслуживания = " << (double)serviced / N << endl;
    cout << "Вероятность отказа = " << (double)cancelled / N << endl;
    cout << "Вероятность занятости одной колонки = " << veronebusy << endl;
    cout << "Вероятность занятости двух колонок = " << time_K2_and_K1_are_not_free() / time << endl;
    cout << "Среднее число занятых колонок = "
        << veronebusy + 2 * time_K2_and_K1_are_not_free() / time << endl;
    cout << "Вероятность простоя КО1 = " << 1 - countTimeko1 / time << endl;
    cout << "Вероятность простоя КО2 = " << 1 - countTimeko2 / time << endl;
    cout << "Среднее количество заявок в очереди = " << totalQueueTime / time << endl;
    cout << "Среднее время ожидания в очереди = " << totalQueueTime / queuecount << endl;
    cout << "Среднее время обслуживания = " << (countTimeko1 + countTimeko2) / serviced << endl;
    cout << "Среднее время заявки в системе = "
        << (countTimeko1 + countTimeko2 + totalQueueTime) / serviced << endl;
    cout << "Среднее количество заявок в системе = "
        << (countTimeko1 + countTimeko2 + totalQueueTime) / time << endl;


    ofstream file("results.csv");
    file << fixed << setprecision(6);

    if (!file.is_open()) {
        cout << "Ошибка: не удалось создать файл results.csv" << endl;
        return;
    }

    file << "a," << serviced / time << ",car/hour\n";
    file << "p_service," << (double)serviced / N << ",probability\n";
    file << "p_reject," << (double)cancelled / N << ",probability\n";
    file << "p_busy_ko1," << veronebusy << ",probability\n";
    file << "p_busy_ko2," << time_K2_and_K1_are_not_free() / time << ",probability\n";
    file << "n_columns_avg," << veronebusy + 2 * time_K2_and_K1_are_not_free() / time << ",cars\n";
    file << "p_idle_ko1," << 1 - countTimeko1 / time << ",probability\n";
    file << "p_idle_ko2," << 1 - countTimeko2 / time << ",probability\n";
    file << "r_avg," << totalQueueTime / time << ",cars\n";
    file << "t_wait_avg," << totalQueueTime / queuecount << ",hour\n";
    file << "t_service_avg," << (countTimeko1 + countTimeko2) / serviced << ",hour\n";
    file << "t_system_avg," << (countTimeko1 + countTimeko2 + totalQueueTime) / serviced << ",hour\n";
    file << "n_avg," << (countTimeko1 + countTimeko2 + totalQueueTime) / time << ",cars\n";

    file.close();
    cout << "\nРезультаты сохранены в файл results.csv" << endl;

}

int main()
{
    srand(time(NULL));
    setlocale(LC_ALL, "Russian");

    generate_z();
    obrab();

    computeValues();

    return 0;
}