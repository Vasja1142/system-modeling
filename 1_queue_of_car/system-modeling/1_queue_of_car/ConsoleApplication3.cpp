#include <iostream>
#include <vector>
#include <random>
#include <cmath>
#include <fstream>

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
                ko1.push_back(make_pair(ko1last, ko1Value));

                c.ko1 = ko1Value;

                c.queue = mo[mo.size() - 1].second - mo[mo.size() - 1].first;

                cars.push_back(c);
                continue;
            }
            if (ko1last > ko2last) {
                mo.push_back(make_pair(z[i], ko2last));
                ko2.push_back(make_pair(ko2last, ko2Value));

                c.ko2 = ko2Value;

                c.queue = mo[mo.size() - 1].second - mo[mo.size() - 1].first;

                cars.push_back(c);
                continue;
            }
        }
        if (mo.size() != 0 && mo[mo.size() - 1].second <= z[i]) {

            if (ko1last <= ko2last) {
                mo.push_back(make_pair(z[i], ko1last));
                ko1.push_back(make_pair(ko1last, ko1Value));

                c.queue = mo[mo.size() - 1].second - mo[mo.size() - 1].first;

                c.ko1 = ko1Value;

                cars.push_back(c);
                continue;
            }
            if (ko1last > ko2last) {
                mo.push_back(make_pair(z[i], ko2last));
                ko2.push_back(make_pair(ko2last, ko2Value));

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

double probability_K2_and_K1_are_not_free() {
    double totalOverlapTime = 0;
    double totalTime = 0;

    if (ko1.empty() || ko2.empty()) return 0;

    double startTime = min(ko1[0].first, ko2[0].first);
    double endTime = max(ko1.back().second, ko2.back().second);
    totalTime = endTime - startTime;

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

    if (totalTime > 0) {
        return totalOverlapTime / totalTime;
    }
    return 0;
}


void saveResultsToCSV(vector<double>& vect) {

    ofstream file("results.csv");

    if (!file.is_open()) {
        return;
    }
    file << "parameter_name,value,unit\n";
    file << "a," << vect[0] << ",car/hour\n";
    file << "p_service," << vect[1] << ",probability\n";
    file << "p_reject," << vect[2] << ",probability\n";
    file << "p_busy_ko1," << vect[3] << ",probability\n";
    file << "p_busy_ko2," << vect[4] << ",probability\n";
    file << "n_columns_avg," << vect[5] << ",cars\n";
    file << "p_idle_ko1," << vect[6] << ",probability\n";
    file << "p_idle_ko2," << vect[7] << ",probability\n";
    file << "r_avg," << vect[8] << ",cars\n";
    file << "t_wait_avg," << vect[9] << ",hour\n";
    file << "t_service_avg," << vect[10] << ",hour\n";
    file << "t_system_avg," << vect[11] << ",hour\n";
    file << "n_avg," << vect[12] << ",cars\n";
    file.close();
}


void computeValues() {
    vector<double> vect;
    double time = cars[999].timeDeparture() - cars[0].timeArrival;
    int cancelled = 0;
    int serviced = 0;
    double countTimeko1 = 0;
    double countTimeko2 = 0;
    double totalQueueTime = 0;
    int queuecount = 0;
    int ko1count = 0, ko2count = 0;

    for (int i = 0; i < 1000; i++) {
        if (cars[i].isCancelled()) {
            cancelled++;
        }
        else {
            serviced++;
        }
        if (cars[i].ko1 != 0) {
            ko1count++;
            countTimeko1 += cars[i].ko1;
        }
        if (cars[i].ko2 != 0) {
            ko2count++;
            countTimeko2 += cars[i].ko2;
        }
        if (cars[i].queue != 0) {
            totalQueueTime += cars[i].queue;
            queuecount++;
        }
    }

    double probabilityBothBusy = probability_K2_and_K1_are_not_free();
    double probabilityKo1Busy = countTimeko1 / time;
    double probabilityKo2Busy = countTimeko2 / time;

    vect.push_back(serviced / time);
    vect.push_back((double)serviced / 1000);
    vect.push_back((double)cancelled / 1000);
    vect.push_back(probabilityKo1Busy);
    vect.push_back(probabilityBothBusy);
    vect.push_back(probabilityKo1Busy + 2 * probabilityBothBusy);
    vect.push_back(1 - probabilityKo1Busy);
    vect.push_back(1 - probabilityKo2Busy);
    vect.push_back(totalQueueTime / time);
    vect.push_back(totalQueueTime / time);
    vect.push_back(totalQueueTime / queuecount);
    vect.push_back((countTimeko1 + countTimeko2) / serviced);
    vect.push_back((countTimeko1 + countTimeko2 + totalQueueTime) / serviced);
    vect.push_back((countTimeko1 + countTimeko2 + totalQueueTime) / time);

    saveResultsToCSV(vect);
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
