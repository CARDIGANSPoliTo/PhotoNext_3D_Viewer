import sys
import pandas as pd
import matplotlib
import matplotlib.pylab as p
import matplotlib.pyplot as plt
plt.switch_backend('agg')

print(sys.argv[1])
csv_file = pd.read_csv(sys.argv[1])


p.figure(figsize=(21,18))
value = 0

for col in csv_file.columns.values: #csv_file.i_loc(1,axis=1)
  data = csv_file[col]
  
  
  if 'Timestamp' in col:
    t = data #range(len(data))
    if value == 0:
        value = t.min()
        print(value)
    t = t - value
    print(t)
  else:
    p.plot(t, data, '-', label = col)

  
p.legend(loc='upper left')

plt.savefig(sys.argv[2] + '.png', bbox_inches='tight')

plt.close('all')